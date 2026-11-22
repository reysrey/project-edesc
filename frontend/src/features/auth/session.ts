import { useQuery } from '@tanstack/react-query'
import { api } from '../../shared/api/resources'

export const sessionKey = ['session'] as const
export function useSession() {
  return useQuery({
    queryKey: sessionKey,
    queryFn: ({ signal }) => api.profile(signal),
    retry: false,
    staleTime: 60_000,
  })
}
