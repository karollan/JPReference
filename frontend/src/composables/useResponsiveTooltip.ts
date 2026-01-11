import { computed } from 'vue'
import { useDisplay } from 'vuetify'

/**
 * On touch devices, tooltips require clicking to show/hide and will auto-close when clicking outside
 * On desktop, tooltips show on hover
 */
export function useResponsiveTooltip() {
    const { platform } = useDisplay()

    const isMobile = computed(() => platform.value.android || platform.value.ios || platform.value.touch)

    const activatorMode = computed(() => {
        return isMobile.value ? 'click' : undefined
    })

    return {
        isMobile,
        activatorMode,
    }
}
