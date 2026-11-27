export type IconName =
  | 'settings'
  | 'clock'
  | 'shield'
  | 'diamond'
  | 'leaf'
  | 'plus'
  | 'file'
  | 'hourglass'
  | 'dot'
  | 'rotate'
  | 'check'
  | 'star'
  | 'x'
  | 'heart'
  | 'bell'

export function Icon({ name }: { name: IconName }) {
  return (
    <svg className="ui-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
      <use href={`/ideahub-icons.svg#${name}`} />
    </svg>
  )
}
