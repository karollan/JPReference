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
          primary: '#FF6659',
          background: '#1E1E1E',
          surface: '#2C2C2C',
          secondary: '#B0BEC5',
          accent: '#FFD54F',
        }
      }
    }
  },
})
