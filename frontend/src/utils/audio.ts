export function playPronunciation(text: string) {
    window.speechSynthesis.cancel() // avoid overlapping and stacking pronunciations

    const utterance = new SpeechSynthesisUtterance(text)
    utterance.lang = 'ja-JP'

    const findJapaneseVoice = () => {
        const voices = window.speechSynthesis.getVoices()
        let japaneseVoice = voices.find(v => v.voiceURI === 'Google 日本語')

        // any japanese voice fallback
        if (!japaneseVoice) {
            japaneseVoice = voices.find(v => v.lang.startsWith('ja') || v.name.includes('Japanese'))
        }

        // any voice fallback
        if (!japaneseVoice) {
            japaneseVoice = voices[0]
        }

        return japaneseVoice
    }

    const japaneseVoice = findJapaneseVoice()

    if (japaneseVoice) {
        utterance.voice = japaneseVoice
        speechSynthesis.speak(utterance)
    } else {
        window.speechSynthesis.onvoiceschanged = () => {
            const loadedJapaneseVoice = findJapaneseVoice()
            if (loadedJapaneseVoice) {
                utterance.voice = loadedJapaneseVoice
                speechSynthesis.speak(utterance)
            }
            window.speechSynthesis.onvoiceschanged = null
        }

        // In case onvoiceschanged never fires, try again with timeout
        setTimeout(() => {
            if (window.speechSynthesis.onvoiceschanged) {
                const loadedJapaneseVoice = findJapaneseVoice()
                if (loadedJapaneseVoice) {
                    utterance.voice = loadedJapaneseVoice
                    speechSynthesis.speak(utterance)
                }
                window.speechSynthesis.onvoiceschanged = null
            }
        }, 1000)
    }
}