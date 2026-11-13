// Shown for any address the app has no page for.
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'

export function NotFoundPage() {
  return (
    <section className="flex flex-col items-start gap-4">
      <p className="label-caps text-muted-foreground">Error 404</p>
      <h1 className="text-5xl">This lane is closed.</h1>
      <p className="max-w-prose text-muted-foreground">There is no page at this address.</p>
      <Button render={<Link to="/" />} nativeButton={false}>
        Back to the gate
      </Button>
    </section>
  )
}
