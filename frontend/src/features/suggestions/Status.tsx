import type { Suggestion } from '../../shared/api/contracts'
import { statusConfig } from './model'

export function Status({ status }: { status: Suggestion['status'] }) { return <span className={`status ${statusConfig[status].className}`}>{statusConfig[status].label}</span> }
