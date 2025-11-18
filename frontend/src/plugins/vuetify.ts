/**
 * plugins/vuetify.ts
 *
 * Framework documentation: https://vuetifyjs.com`
 */

// Styles
import '@mdi/font/css/materialdesignicons.css'
import 'vuetify/styles'

// Composables
import { createVuetify } from 'vuetify'

// https://vuetifyjs.com/en/introduction/why-vuetify/#feature-guides
export default createVuetify({
  theme: {
    defaultTheme: 'jlptTheme',
    themes: {
      jlptTheme: {
        dark: false,
        colors: {
          primary: '#607AFB',
          secondary: '#37474F',
          accent: '#FFB300',
          background: '#f5f6f8',
          surface: '#FFFFFF',
          info: '#2196F3',
          success: '#4CAF50',
          warning: '#FFC107',
          error: '#D32F2F',
        },
        variables: {
          'font-family': `'Noto Sans JP', 'Roboto', sans-serif`
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
        }
      }
    }
  },
})
