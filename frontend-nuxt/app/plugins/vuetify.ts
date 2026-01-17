import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'

export default defineNuxtPlugin((nuxtApp) => {
    const vuetify = createVuetify({
        components,
        directives,
        // SSR configuration: tell Vuetify to assume desktop viewport on server
        // This prevents hydration mismatches from useDisplay() returning different values
        ssr: {
            clientWidth: 1280,
            clientHeight: 800,
        },
        theme: {
            defaultTheme: 'jlptTheme',
            themes: {
                jlptTheme: {
                    dark: false,
                    colors: {
                        'on-surface': '#111111',
                        'on-background': '#111111',
                        primary: '#607AFB',
                        secondary: '#37474F',
                        accent: '#FFB300',
                        background: '#f5f6f8',
                        surface: '#FFFFFF',
                        info: '#2196F3',
                        success: '#4CAF50',
                        warning: '#FFC107',
                        error: '#D32F2F',
                        'filter-chip': '#EEF2FF',
                        'filter-chip-text': '#4338CA',
                        'filter-popup-bg': '#FFFFFF',
                        'filter-popup-border': '#E2E8F0',
                        'filter-popup-item': '#1E293B',
                        'filter-popup-item-hover': '#F1F5F9',
                        'filter-popup-item-selected': '#EEF2FF',
                        'filter-popup-prefix': '#7C3AED',
                    }
                },
                jlptThemeDark: {
                    dark: true,
                    colors: {
                        primary: '#607AFB',
                        'on-primary': '#FFFFFF',
                        secondary: '#B0BEC5',
                        'on-secondary': '#000000',
                        accent: '#FFD54F',
                        'on-accent': '#000000',
                        background: '#121212',
                        'on-background': '#E0E0E0',
                        surface: '#1E1E1E',
                        'on-surface': '#E0E0E0',
                        info: '#2196F3',
                        success: '#4CAF50',
                        warning: '#FFC107',
                        error: '#CF6679',
                        'filter-chip': '#312E81',
                        'filter-chip-text': '#C7D2FE',
                        'filter-popup-bg': '#1E1E2E',
                        'filter-popup-border': '#45475A',
                        'filter-popup-item': '#CDD6F4',
                        'filter-popup-item-hover': '#313244',
                        'filter-popup-item-selected': '#45475A',
                        'filter-popup-prefix': '#C4B5FD',
                    }
                }
            }
        }
    })

    nuxtApp.vueApp.use(vuetify)
})