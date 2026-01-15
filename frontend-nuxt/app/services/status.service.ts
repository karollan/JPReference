import type { Status } from '@/types/Status'

export const useStatusService = () => {
    const config = useRuntimeConfig()

    const baseUrl = import.meta.server ? config.apiUrl : config.public.apiUrl

    return {
        getDatabaseStatus: () => $fetch<Status>(`${baseUrl}/status`)
    }
}