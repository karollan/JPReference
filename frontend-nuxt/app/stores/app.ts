// Utilities
import { defineStore } from 'pinia'

export const useAppStore = defineStore('app', () => {
  const strokePlayer = reactive({
    speed: 5,
    showStrokeNumbers: false
  })

  return {
    strokePlayer
  }
})

