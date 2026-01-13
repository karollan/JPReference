// Server middleware to proxy /kanjivg/* requests to the backend
// This avoids placing thousands of SVG files in the Nuxt public directory
// which causes slow startup due to Nuxt scanning/watching them

export default defineEventHandler(async (event) => {
    const url = getRequestURL(event)

    // Only handle /kanjivg/* requests
    if (!url.pathname.startsWith('/kanjivg/')) {
        return
    }

    const filename = url.pathname.replace('/kanjivg/', '')

    if (!filename || !filename.endsWith('.svg')) {
        return
    }

    // For server-side requests inside Docker, use the internal network hostname
    // The public API URL (localhost:5000) doesn't work from within the container
    const backendUrl = 'http://backend:5000'

    try {
        // Fetch SVG from backend as text (SVG is XML text, not binary)
        const svgContent = await $fetch<string>(`${backendUrl}/kanjivg/${filename}`, {
            responseType: 'text',
        })

        // Set appropriate headers for SVG
        setHeader(event, 'Content-Type', 'image/svg+xml')
        setHeader(event, 'Cache-Control', 'public, max-age=31536000, immutable')

        return svgContent
    } catch (error: any) {
        console.error('Failed to proxy kanjivg:', error?.message)
        throw createError({
            statusCode: error?.statusCode || 404,
            statusMessage: 'SVG not found'
        })
    }
})
