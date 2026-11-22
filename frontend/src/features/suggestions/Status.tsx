import type { Suggestion } from '../../shared/api/contracts'
import { statusConfig } from './model'
import { Icon } from '../../shared/ui/Icon'
import type { IconName } from '../../shared/ui/Icon'

const icons: Record<Suggestion['status'], IconName> = {
  Draft: 'file',
  Submitted: 'dot',
  ReturnedByManager: 'rotate',
  InCommission: 'clock',
  RevisionRequired: 'rotate',
  Rejected: 'x',
  ApprovedPlanning: 'check',
  InImplementation: 'hourglass',
  Implemented: 'star',
}

export function Status({ status }: { status: Suggestion['status'] }) {
  return (
    <span className={`status ${statusConfig[status].className}`}>
      <Icon name={icons[status]} />
      {statusConfig[status].label}
    </span>
  )
}
