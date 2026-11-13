// GLSL source for the silk background: domain-warped cloth that folds and catches light, a fine mesh riding the same folds.
// Adapted from the 21st.dev "Silk mesh shader hero": pulse rings and film grain removed, intro fades in from the
// darkest palette colour (not from black) so it also works on the light theme.

export const VERTEX_SHADER = 'attribute vec2 p;void main(){gl_Position=vec4(p,0.,1.);}'

export const FRAGMENT_SHADER = [
  'precision highp float;',
  'uniform vec2 uRes;uniform float uTime;uniform vec2 uMouse;uniform float uHover;uniform float uIntro;',
  'uniform vec3 uC0;uniform vec3 uC1;uniform vec3 uC2;uniform vec3 uC3;',
  'uniform vec2 uClick;uniform float uClickAge;',
  // the cloth: seven rounds of sine warping give believable folds
  'float silk(vec2 p,float t){vec2 w=p*2.1;for(int i=1;i<7;i++){float fi=float(i);w+=vec2(.62/fi*sin(fi*w.y*.85+t*.32+fi*1.7),.5/fi*cos(fi*w.x*.75-t*.26+fi*.9));}return .5+.5*sin(w.x*1.9+w.y*1.2);}',
  'void main(){',
  'vec2 uv=gl_FragCoord.xy/uRes;vec2 p=(gl_FragCoord.xy-.5*uRes)/uRes.y;float t=uTime;',
  // the cloth leans away from the pointer
  'vec2 dm=p-uMouse;float md=dot(dm,dm);p+=dm*.22*exp(-md*7.)*uHover;',
  // a click ripple travels outwards and fades
  'vec2 dc=p-uClick;float cd=length(dc);float wave=sin(cd*26.-uClickAge*9.)*exp(-uClickAge*1.1)*smoothstep(uClickAge*.75+.05,uClickAge*.75-.25,cd);',
  'p+=normalize(dc+1e-4)*wave*.018;',
  // lighting from the slope of the folds
  'float h0=silk(p,t);vec2 e=vec2(.004,0.);float hx=silk(p+e.xy,t)-h0;float hy=silk(p+e.yx,t)-h0;',
  'vec3 n=normalize(vec3(-hx/e.x*.26,-hy/e.x*.26,1.));vec3 L=normalize(vec3(-.45,.6,.75));',
  'float dif=clamp(dot(n,L),0.,1.);float spec=pow(clamp(dot(reflect(-L,n),vec3(0.,0.,1.)),0.,1.),16.);',
  'vec3 col=mix(uC0,uC1,smoothstep(.0,.85,h0));',
  'col=mix(col,uC2,smoothstep(.35,1.,dif)*.85);',
  'col=mix(col,uC0,smoothstep(.45,.0,dif)*.55);',
  // mixed toward the highlight token, never added: adding pushed the light theme past white (blown-out glare)
  'col=mix(col,uC3,spec*.6);',
  'vec2 r=vec2(hx,hy)*40.;',
  // the mesh: a fine grid displaced by the same folds, strongest low in the frame
  'vec2 g=(p+r*.02+n.xy*.05)*vec2(15.,15.);vec2 gd=abs(fract(g)-.5);float line=1.-smoothstep(0.,.05,min(gd.x,gd.y));',
  'col=mix(col,uC3,line*.14*(.25+.75*smoothstep(.8,.0,uv.y))*(.3+.7*dif));',
  'col+=uC3*smoothstep(.02,0.,abs(cd-uClickAge*.75))*exp(-uClickAge*1.6)*.25;',
  // vignette keeps the edges calm so the content in the middle reads first
  'float vig=smoothstep(1.25,.35,length((uv-.5)*vec2(1.1,1.3)));col=mix(uC0,col,mix(.55,1.,vig));',
  'gl_FragColor=vec4(mix(uC0,col,uIntro),1.);}',
].join('')
