export function playPronunciation(text: string) {
    const utterance = new SpeechSynthesisUtterance(text)
    utterance.voice = window.speechSynthesis.getVoices().find(v => v.voiceURI === 'Google 日本語')
    speechSynthesis.speak(utterance)
}