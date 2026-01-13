import type { Status } from '@/types/Status'
import { getApiUrl } from './api'

export const StatusService = {
    async getDatabaseStatus(): Promise<Status> {
        const data = await $fetch<Status>(`${getApiUrl()}/Status`)
        return data
    },
}
