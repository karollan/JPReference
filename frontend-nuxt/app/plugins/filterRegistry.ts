/**
 * Plugin to fetch filter registry from backend on app initialization.
 * This ensures the registry is synced before any component uses it.
 * Falls back to hardcoded registry if fetch fails.
 * 
 * NOTE: Only runs on client-side to avoid SSR issues with relative API paths.
 */
import { setFilterRegistry, type FilterRegistryEntry } from '@/utils/filters'

export default defineNuxtPlugin(async () => {
    // Only run on client-side - server uses fallback registry
    if (import.meta.server) {
        return
    }

    try {
        // Client-side: use relative /api path (nginx proxies to backend)
        const registry = await $fetch<FilterRegistryEntry[]>('/api/Filters/registry')
        setFilterRegistry(registry)
        console.log(`[filterRegistry] Synced ${registry.length} filters from backend`)
    } catch (e) {
        console.warn('[filterRegistry] Failed to fetch registry, using fallback:', e)
        // Fallback registry will be used automatically via getFilterRegistry()
    }
})
