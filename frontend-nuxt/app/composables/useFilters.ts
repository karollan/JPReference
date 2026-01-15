/**
 * Composable for accessing filter definitions.
 * Uses the synced registry from filters.ts (populated via plugin on app load).
 */
import { getFilterRegistry, isRegistrySynced, type FilterDefinition, type DataType } from '@/utils/filters'

export interface FilterInfo {
    key: string
    type: string
    description?: string
    appliesTo?: DataType[]
}

/**
 * Provides access to filter definitions from the synced registry.
 * This uses the registry that was populated on app load by the filterRegistry plugin.
 */
export function useFilters() {
    // Get all filters from the synced registry
    const allFilters = computed<FilterInfo[]>(() => {
        const registry = getFilterRegistry()
        return Array.from(registry.values()).map(f => ({
            key: f.key,
            type: f.type,
            description: f.description,
            appliesTo: f.appliesTo
        }))
    })

    // Filter registry synced status
    const isSynced = computed(() => isRegistrySynced())

    /**
     * Get all filters.
     */
    const filters = computed(() => allFilters.value)

    /**
     * Get tag filters (boolean type only).
     */
    const tagFilters = computed(() =>
        allFilters.value.filter(f => f.type === 'boolean')
    )

    /**
     * Get static filters (non-boolean).
     */
    const staticFilters = computed(() =>
        allFilters.value.filter(f => f.type !== 'boolean')
    )

    /**
     * Get filters grouped by what data type they apply to.
     */
    const filtersByDataType = computed(() => ({
        kanji: allFilters.value.filter(f => f.appliesTo?.includes('kanji')),
        vocabulary: allFilters.value.filter(f => f.appliesTo?.includes('vocabulary')),
        properNoun: allFilters.value.filter(f => f.appliesTo?.includes('properNoun'))
    }))

    return {
        filters,
        tagFilters,
        staticFilters,
        filtersByDataType,
        isSynced,
        status: computed(() => isSynced.value ? 'success' : 'pending'),
        error: computed(() => null)
    }
}
