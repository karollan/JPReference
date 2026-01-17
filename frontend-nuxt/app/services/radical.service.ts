import type { RadicalSearchResult, RadicalSummary, RadicalDetails } from '@/types/Radical'

export const useRadicalService = () => {
    const config = useRuntimeConfig()

    const baseUrl = import.meta.server ? config.apiUrl : config.public.apiUrl

    return {
        getRadicalsList: () => $fetch<RadicalSummary[]>(`${baseUrl}/radical/list`),
        searchKanjiByRadicals: (radicalIds: string[]) => $fetch<RadicalSearchResult>(`${baseUrl}/radical/search`, {
            method: 'POST', body: radicalIds
        }),
        fetchRadicalByLiteral: (literal: string) => $fetch<RadicalDetails>(`${baseUrl}/radical/${encodeURIComponent(literal)}`)
    }
}