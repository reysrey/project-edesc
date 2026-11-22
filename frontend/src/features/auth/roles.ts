import type { Profile } from '../../shared/api/contracts'

// AdminL1 is assigned to the top-level account by the existing backend seed.
// Keep wire names stable; only the product labels change.
export function roleOf(profile: Profile) {
  if (profile.roles.includes('AdminL1')) return 'leader'
  if (profile.roles.includes('AdminL2')) return 'moderator'
  return 'employee'
}
export const roleLabels = {
  employee: 'Сотрудник',
  moderator: 'Модератор',
  leader: 'Руководитель',
} as const
export const isLeader = (profile: Profile) => roleOf(profile) === 'leader'
export const canModerate = (profile: Profile) => roleOf(profile) !== 'employee'
