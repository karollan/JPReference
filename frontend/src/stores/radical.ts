import type { RadicalDetails } from '@/types/Radical'
// Utilities
import { defineStore } from 'pinia'
import { RadicalService } from '@/services/radical.service'

export const useRadicalStore = defineStore('radical', () => {
    // State
    const radicalDetails = ref<RadicalDetails | null>(null)
    const radicalDetailsCache = reactive<{ [literal: string]: RadicalDetails }>({})
    const loading = ref<boolean>(false)
    const error = ref<string | null>(null)

    // Utils

    // Actions
    const getRadicalByLiteral = async (
        literal: string,
    ) => {
        const foundRadical = getRadicalCache(literal)
        if (foundRadical) {
            radicalDetails.value = foundRadical
            return foundRadical
        }
        loading.value = true
        error.value = null

        try {
            const response = await RadicalService.fetchRadicalByLiteral(literal)

            // Validate response structure
            if (!response) {
                throw new Error('Radical not found')
            }

            // Cache hygiene
            if (Object.keys(radicalDetailsCache).length > 10) {
                delete radicalDetailsCache[Object.keys(radicalDetailsCache)[0]!]
            }

            // Cache the response
            radicalDetailsCache[literal] = response
            radicalDetails.value = response
            return response
        } catch (error_: any) {
            console.error('RadicalStore error:', error_)
            error.value = error_.message ?? 'Failed to fetch radical'
            radicalDetails.value = null
        } finally {
            loading.value = false
        }
    }

    // Getters
    const getRadicalCache = (literal: string) => {
        return radicalDetailsCache[literal]
    }

    return {
        // Expose state, actions, getters here
        radicalDetails,
        radicalDetailsCache,
        error,
        loading,
        getRadicalByLiteral,
    }
})
