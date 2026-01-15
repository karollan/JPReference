/**
 * Composable for fetching and caching filter definitions from the backend.
 * Provides both static filters and dynamic tag-based filters.
 */
import type { DataType } from '@/utils/filters'

export interface BackendFilterDefinition {
    key: string
    type: string
    description: string
    appliesTo: DataType[]
    min?: number
    max?: number
}

export interface FiltersResponse {
    staticFilters: BackendFilterDefinition[]
    tagFilters: BackendFilterDefinition[]
}

/**
 * Fetches and caches filter definitions from the backend.
 * Returns both static (built-in) filters and tag-based filters.
 */
export function useFilters() {
    const config = useRuntimeConfig()
    const baseUrl = config.public.apiBase || 'http://localhost:5000'

    const { data: filters, status, error, refresh } = useAsyncData<FiltersResponse>(
        'filters',
        () => $fetch<FiltersResponse>(`${baseUrl}/api/Filters`),
        {
            // Cache for 1 hour - filter definitions rarely change
            getCachedData: (key, nuxtApp) => {
                return nuxtApp.payload.data[key] || nuxtApp.static.data[key]
            }
        }
    )

    /**
     * Get all filters grouped by what data type they apply to.
     */
    const filtersByDataType = computed(() => {
        if (!filters.value) return { kanji: [], vocabulary: [], properNoun: [] }

        const allFilters = [...filters.value.staticFilters, ...filters.value.tagFilters]

        return {
            kanji: allFilters.filter(f => f.appliesTo.includes('kanji')),
            vocabulary: allFilters.filter(f => f.appliesTo.includes('vocabulary')),
            properNoun: allFilters.filter(f => f.appliesTo.includes('properNoun'))
        }
    })

    /**
     * Get all tag filters (for autocomplete suggestions).
     */
    const tagFilters = computed(() => filters.value?.tagFilters || [])

    /**
     * Get all static filters.
     */
    const staticFilters = computed(() => filters.value?.staticFilters || [])

    return {
        filters,
        filtersByDataType,
        tagFilters,
        staticFilters,
        status,
        error,
        refresh
    }
}
