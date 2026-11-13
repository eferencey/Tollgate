// A destructive button that asks first, in place: the trigger turns into "question + Yes / Cancel" (no dialog window).
// Focus lands on Cancel, the safe choice, and Esc backs out. While working it says so; a failure is announced.
import { useEffect, useRef, useState, type ReactNode } from 'react'
import { Button } from '@/components/ui/button'

type Step = 'idle' | 'asking' | 'working' | 'failed'

/** Touch targets of at least 44 px on phones, the button's own height from `sm` up. */
const TOUCH = 'min-h-11 min-w-11 sm:min-h-10 sm:min-w-10'

interface ConfirmActionProps {
  /** The trigger's content (text or an icon). */
  trigger: ReactNode
  /** Accessible name of the trigger; needed when the trigger is only an icon. */
  triggerLabel?: string
  question: string
  confirmLabel: string
  failureText: string
  /** Resolves to false when the action failed. */
  onConfirm: () => Promise<boolean>
  className?: string
}

export function ConfirmAction({ trigger, triggerLabel, question, confirmLabel, failureText, onConfirm, className }: ConfirmActionProps) {
  const [step, setStep] = useState<Step>('idle')
  const cancelRef = useRef<HTMLButtonElement>(null)
  const triggerRef = useRef<HTMLButtonElement>(null)
  const returnFocus = useRef(false)

  useEffect(() => {
    if (step === 'asking') cancelRef.current?.focus()
    if (step === 'idle' && returnFocus.current) {
      returnFocus.current = false
      triggerRef.current?.focus()
    }
  }, [step])

  const cancel = () => {
    returnFocus.current = true
    setStep('idle')
  }

  const confirm = async () => {
    setStep('working')
    // On success this component usually disappears with its row; on failure it stays and says so.
    setStep((await onConfirm()) ? 'idle' : 'failed')
  }

  if (step === 'idle') {
    return (
      <Button ref={triggerRef} variant="outline" aria-label={triggerLabel} onClick={() => setStep('asking')} className={`${TOUCH} ${className ?? ''}`}>
        {trigger}
      </Button>
    )
  }

  return (
    <div
      role="group"
      aria-label={question}
      onKeyDown={(event) => {
        if (event.key === 'Escape' && step !== 'working') cancel()
      }}
      className="flex flex-wrap items-center gap-x-3 gap-y-2"
    >
      <p className="text-sm">{question}</p>
      <div className="flex gap-2">
        <Button variant="destructive" onClick={() => void confirm()} disabled={step === 'working'} className={TOUCH}>
          {step === 'working' ? 'Deleting…' : confirmLabel}
        </Button>
        <Button ref={cancelRef} variant="outline" onClick={cancel} disabled={step === 'working'} className={TOUCH}>
          Cancel
        </Button>
      </div>
      {step === 'failed' && (
        <p role="alert" className="basis-full text-sm text-destructive">
          {failureText}
        </p>
      )}
    </div>
  )
}
