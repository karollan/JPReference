export const fetchWithError = async<T>(fn: () => Promise<T>) => {
    try {
        return await fn()
    } catch (e: any) {
        throw createError({
            statusCode: e?.statusCode ?? 500,
            message: e?.data?.message ?? 'Not found',
        })
    }
}