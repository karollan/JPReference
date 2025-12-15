/**
 * plugins/vuetify.ts
 *
 * Framework documentation: https://vuetifyjs.com`
 */

// Composables
import { createVuetify } from 'vuetify'

// Styles
import '@mdi/font/css/materialdesignicons.css'
import 'vuetify/styles'

// https://vuetifyjs.com/en/introduction/why-vuetify/#feature-guides
export default createVuetify({
  theme: {
    defaultTheme: 'jlptTheme',
    themes: {
      jlptTheme: {
        dark: false,
        colors: {
          'primary': '#607AFB',
          'secondary': '#37474F',
          'accent': '#FFB300',
          'background': '#f5f6f8',
          'surface': '#FFFFFF',
          'info': '#2196F3',
          'success': '#4CAF50',
          'warning': '#FFC107',
          'error': '#D32F2F',
          // Filter autocomplete colors
          'filter-chip': '#EEF2FF',
          'filter-chip-text': '#4338CA',
          'filter-popup-bg': '#FFFFFF',
          'filter-popup-border': '#E2E8F0',
          'filter-popup-item': '#1E293B',
          'filter-popup-item-hover': '#F1F5F9',
          'filter-popup-item-selected': '#EEF2FF',
          'filter-popup-prefix': '#7C3AED',
        },
        variables: {
          'font-family': `'Noto Sans JP', 'Roboto', sans-serif`,
        },
      },
      jlptThemeDark: {
        dark: true,
        colors: {
          'primary': '#607AFB',
          'on-primary': '#FFFFFF',
          'secondary': '#B0BEC5',
          'on-secondary': '#000000',
          'accent': '#FFD54F',
          'on-accent': '#000000',
          'background': '#121212',
          'on-background': '#E0E0E0',
          'surface': '#1E1E1E',
          'on-surface': '#E0E0E0',
          'info': '#2196F3',
          'success': '#4CAF50',
          'warning': '#FFC107',
          'error': '#CF6679',
          // Filter autocomplete colors
          'filter-chip': '#312E81',
          'filter-chip-text': '#C7D2FE',
          'filter-popup-bg': '#1E1E2E',
          'filter-popup-border': '#45475A',
          'filter-popup-item': '#CDD6F4',
          'filter-popup-item-hover': '#313244',
          'filter-popup-item-selected': '#45475A',
          'filter-popup-prefix': '#C4B5FD',
        },
      },
    },
  },
})
