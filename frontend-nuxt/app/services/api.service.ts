export const useApiService = () => {
    const config = useRuntimeConfig()

    const baseUrl: string = import.meta.server ? config.apiUrl as string : config.public.apiUrl
    const baseUrlNoApi = baseUrl.replace('/api', '')
    return {
        getApiSpec: () => $fetch<any>(`${baseUrlNoApi}/swagger/v1/swagger.json`)
    }
} 