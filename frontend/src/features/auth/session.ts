import { useQuery } from '@tanstack/react-query'
import { api } from '../../shared/api/resources'
import type { Profile } from '../../shared/api/contracts'

export const sessionKey = ['session'] as const
export function useSession() {
  return useQuery({
    queryKey: sessionKey,
    queryFn: ({ signal }) => api.profile(signal),
    retry: false,
    staleTime: 60_000,
  })
}
export function isManager(profile: Profile) {
  return profile.roles.some((role) => role === 'AdminL1' || role === 'AdminL2')
}
