<template>
  <div class="search-autocomplete-wrapper">
    <div class="search-input-container" :class="{ 'is-focused': isFocused, 'has-error': hasValidationError }">
      <v-icon class="search-icon" size="20">mdi-magnify</v-icon>
      <div
        ref="editableRef"
        class="search-editable"
        contenteditable="true"
        :data-placeholder="placeholder || 'Search'"
        @blur="handleBlur"
        @click="handleClick"
        @focus="handleFocus"
        @input="handleInput"
        @keydown="handleKeydown"
      />
      <button
        v-if="searchQuery"
        class="clear-btn"
        type="button"
        @mousedown.prevent="handleClear"
      >
        <v-icon size="18">mdi-close-circle</v-icon>
      </button>
    </div>
    <Teleport to="body">
      <div
        v-if="showPopup"
        ref="popupRef"
        class="filter-popup"
        :style="popupStyle"
      >
        <div class="filter-popup-list">
          <div
            v-for="(suggestion, index) in suggestions"
            :key="`${suggestion.type}-${index}-${suggestion.value}`"
            class="filter-popup-item"
            :class="{
              'filter-popup-item--selected': index === selectedIndex,
              'filter-popup-item--hint': suggestion.type === 'hint',
            }"
            @mousedown.prevent="selectSuggestion(suggestion)"
          >
            <span v-if="suggestion.type === 'filter-key'" class="filter-prefix">#</span>
            <span v-if="suggestion.type === 'operator'" class="filter-prefix">:</span>
            <span class="filter-label">{{ suggestion.label }}</span>
            <span v-if="suggestion.description" class="filter-description">{{ suggestion.description }}</span>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
  import type { FilterParseState } from '@/utils/filterParser'
  import type { Suggestion } from '@/utils/filterSuggestions'
  import { parseFilterState } from '@/utils/filterParser'
  import { getFilterDefinition } from '@/utils/filters'
  import { generateSuggestions } from '@/utils/filterSuggestions'
  import { getFilterValidationError } from '@/utils/filterValidation'

  interface Props {
    placeholder?: string
  }

  defineProps<Props>()

  const editableRef = ref<HTMLDivElement | null>(null)
  const popupRef = ref<HTMLElement | null>(null)
  const isFocused = ref(false)
  const suggestions = ref<Suggestion[]>([])
  const selectedIndex = ref(0)
  const filterState = ref<FilterParseState | null>(null)
  const popupPosition = ref({ top: 0, left: 0 })
  // Track committed chip positions (start index in text) to preserve chips even without trailing space
  const committedChipPositions = ref<Set<number>>(new Set())
  const hasValidationError = ref(false)

  const searchQuery = defineModel<string>('searchQuery', { required: true })

  const emit = defineEmits<{
    clear: []
    search: []
  }>()

  const showPopup = computed(() => isFocused.value && suggestions.value.length > 0)

  const popupStyle = computed(() => ({
    position: 'fixed' as const,
    top: `${popupPosition.value.top}px`,
    left: `${popupPosition.value.left}px`,
    zIndex: 9999,
  }))

  // Render the editable content with filter tags
  // Chips are created when: followed by space, selected from popup, or on blur
  // Once committed, chips stay as chips until edited
  function renderContent (forceCommitAll = false) {
    if (!editableRef.value) return

    const text = searchQuery.value || ''
    if (!text) {
      editableRef.value.innerHTML = ''
      committedChipPositions.value.clear()
      return
    }

    // Parse all filters in the text
    const segments: Array<{ type: 'text' | 'filter', content: string, start: number }> = []
    const newCommittedPositions = new Set<number>()

    // Use regex to find all potential filters
    const regex = /#([^\s#]+)/g
    let lastIndex = 0
    let match

    while ((match = regex.exec(text)) !== null) {
      const fullMatch = match[0]
      const filterText = match[1] || ''
      const matchStart = match.index

      // Add text before match
      if (matchStart > lastIndex) {
        segments.push({ type: 'text', content: text.slice(lastIndex, matchStart), start: lastIndex })
      }

      // Check if this filter is complete and valid
      const hasSpaceAfter = text[matchStart + fullMatch.length] === ' '
      const wasAlreadyCommitted = committedChipPositions.value.has(matchStart)
      const shouldBeChip = hasSpaceAfter || wasAlreadyCommitted || forceCommitAll

      if (shouldBeChip) {
        // Parse the filter to check if it's valid
        const colonIndex = filterText.indexOf(':')
        if (colonIndex === -1) {
          // No colon - check if it's a boolean filter
          const def = getFilterDefinition(filterText)
          if (def && def.type === 'boolean') {
            segments.push({ type: 'filter', content: filterText, start: matchStart })
            newCommittedPositions.add(matchStart)
          } else {
            segments.push({ type: 'text', content: fullMatch, start: matchStart })
          }
        } else {
          const key = filterText.slice(0, colonIndex)
          const def = getFilterDefinition(key)

          if (def) {
            // Check if filter is complete
            const value = filterText.slice(colonIndex + 1)
            const dashIndex = value.indexOf('-')

            let isValid = false
            switch (def.type) {
              case 'boolean': {
                isValid = true
                break
              }
              case 'enum':
              case 'equality': {
                const num = Number.parseInt(value, 10)
                isValid = value !== '' && !Number.isNaN(num)
                break
              }
              case 'range': {
                if (dashIndex === -1) {
                  // Single value format: 1 (treated as 1-1)
                  const num = Number.parseInt(value, 10)
                  isValid = value !== '' && !Number.isNaN(num)
                  if (isValid && def.min !== undefined && num < def.min) {
                    isValid = false
                  }
                  if (isValid && def.max !== undefined && num > def.max) {
                    isValid = false
                  }
                } else {
                  // Range format: 1-3
                  const min = Number.parseInt(value.slice(0, dashIndex), 10)
                  const max = Number.parseInt(value.slice(dashIndex + 1), 10)
                  isValid = !Number.isNaN(min) && !Number.isNaN(max) && min <= max
                }
                break
              }
            }

            if (isValid) {
              segments.push({ type: 'filter', content: filterText, start: matchStart })
              newCommittedPositions.add(matchStart)
            } else {
              segments.push({ type: 'text', content: fullMatch, start: matchStart })
            }
          } else {
            segments.push({ type: 'text', content: fullMatch, start: matchStart })
          }
        }
      } else {
        segments.push({ type: 'text', content: fullMatch, start: matchStart })
      }

      lastIndex = matchStart + fullMatch.length
    }

    // Update committed positions
    committedChipPositions.value = newCommittedPositions

    // Add remaining text
    if (lastIndex < text.length) {
      segments.push({ type: 'text', content: text.slice(lastIndex), start: lastIndex })
    }

    // Build HTML
    let html = ''
    for (const segment of segments) {
      html += segment.type === 'filter'
        ? `<span class="filter-chip" data-filter="${escapeHtml(segment.content)}" contenteditable="false">#${escapeHtml(segment.content)}</span>`
        : escapeHtml(segment.content)
    }

    // Save cursor position
    const cursorOffset = getCursorOffset()

    editableRef.value.innerHTML = html

    // Restore cursor position
    if (isFocused.value && cursorOffset !== null) {
      setCursorOffset(cursorOffset)
    }
  }

  function escapeHtml (text: string): string {
    return text
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
  }

  function getCursorOffset (): number | null {
    const selection = globalThis.getSelection()
    if (!selection || !selection.rangeCount || !editableRef.value) return null

    const range = selection.getRangeAt(0)
    const preRange = range.cloneRange()
    preRange.selectNodeContents(editableRef.value)
    preRange.setEnd(range.startContainer, range.startOffset)
    return preRange.toString().length
  }

  function setCursorOffset (offset: number) {
    if (!editableRef.value) return

    const selection = globalThis.getSelection()
    if (!selection) return

    const range = document.createRange()
    let currentOffset = 0
    let found = false

    function walkNodes (node: Node) {
      if (found) return

      if (node.nodeType === Node.TEXT_NODE) {
        const textLength = node.textContent?.length || 0
        if (currentOffset + textLength >= offset) {
          range.setStart(node, offset - currentOffset)
          range.collapse(true)
          found = true
          return
        }
        currentOffset += textLength
      } else if (node.nodeType === Node.ELEMENT_NODE) {
        const el = node as HTMLElement
        // For filter chips, count the full text content
        if (el.classList?.contains('filter-chip')) {
          const chipLength = el.textContent?.length || 0
          if (currentOffset + chipLength >= offset) {
            // Place cursor after the chip
            range.setStartAfter(node)
            range.collapse(true)
            found = true
            return
          }
          currentOffset += chipLength
        } else {
          for (const child of node.childNodes) {
            walkNodes(child)
            if (found) return
          }
        }
      }
    }

    walkNodes(editableRef.value)

    if (!found) {
      // Place at end
      range.selectNodeContents(editableRef.value)
      range.collapse(false)
    }

    selection.removeAllRanges()
    selection.addRange(range)
  }

  function getPlainText (): string {
    if (!editableRef.value) return ''

    let text = ''
    function walkNodes (node: Node) {
      if (node.nodeType === Node.TEXT_NODE) {
        text += node.textContent || ''
      } else if (node.nodeType === Node.ELEMENT_NODE) {
        const el = node as HTMLElement
        if (el.classList?.contains('filter-chip')) {
          text += el.textContent || ''
        } else {
          for (const child of node.childNodes) {
            walkNodes(child)
          }
        }
      }
    }
    walkNodes(editableRef.value)
    return text
  }

  // Calculate popup position anchored to the hashtag character
  function updatePopupPosition () {
    if (!editableRef.value || !filterState.value) return

    const rect = editableRef.value.getBoundingClientRect()

    // Create a temporary span to measure text width up to the hashtag
    const measureSpan = document.createElement('span')
    measureSpan.style.cssText = `
      position: absolute;
      visibility: hidden;
      white-space: pre;
      font: ${getComputedStyle(editableRef.value).font};
    `

    const textBeforeHash = searchQuery.value.slice(0, filterState.value.triggerIndex)
    measureSpan.textContent = textBeforeHash
    document.body.append(measureSpan)
    const textWidth = measureSpan.offsetWidth
    measureSpan.remove()

    popupPosition.value = {
      top: rect.bottom + 4,
      left: rect.left + textWidth,
    }
  }

  function handleFocus () {
    isFocused.value = true
  }

  function handleBlur () {
    isFocused.value = false
    suggestions.value = []
    selectedIndex.value = 0
    filterState.value = null
    hasValidationError.value = false

    // On blur, commit any trailing valid filters by re-rendering with forceCommitAll
    nextTick(() => {
      renderContent(true)
    })
  }

  function handleClick () {
    // Hide popup when clicking on textfield while it's open
    if (showPopup.value) {
      suggestions.value = []
      selectedIndex.value = 0
      filterState.value = null
    }
  }

  function handleInput () {
    const text = getPlainText()
    searchQuery.value = text

    // Re-render to update chips
    const cursorPos = getCursorOffset()
    nextTick(() => {
      renderContent()
      if (cursorPos !== null) {
        setCursorOffset(cursorPos)
      }
    })
  }

  function handleClear () {
    searchQuery.value = ''
    committedChipPositions.value.clear()
    suggestions.value = []
    filterState.value = null
    hasValidationError.value = false
    emit('clear')
    editableRef.value?.focus()
  }

  function findFilterChipAtCursor (): { chip: HTMLElement, start: number, end: number } | null {
    if (!editableRef.value) return null

    const selection = globalThis.getSelection()
    if (!selection || !selection.rangeCount) return null

    const cursorOffset = getCursorOffset()
    if (cursorOffset === null) return null

    // Check if cursor is right after a filter chip
    const chips = editableRef.value.querySelectorAll('.filter-chip')

    for (const chip of chips) {
      // Calculate chip position in plain text
      let chipStart = 0
      let found = false

      function findChipStart (node: Node): boolean {
        if (node === chip) {
          found = true
          return true
        }
        if (node.nodeType === Node.TEXT_NODE) {
          if (!found) chipStart += node.textContent?.length || 0
        } else if (node.nodeType === Node.ELEMENT_NODE) {
          const el = node as HTMLElement
          if (el.classList?.contains('filter-chip') && el !== chip) {
            if (!found) chipStart += el.textContent?.length || 0
          } else {
            for (const child of node.childNodes) {
              if (findChipStart(child)) return true
            }
          }
        }
        return false
      }

      findChipStart(editableRef.value)

      const chipEnd = chipStart + (chip.textContent?.length || 0)

      if (cursorOffset === chipEnd) {
        return { chip: chip as HTMLElement, start: chipStart, end: chipEnd }
      }
    }

    return null
  }

  function handleKeydown (event: KeyboardEvent) {
    // Prevent Enter from creating new lines - trigger search instead
    if (event.key === 'Enter' && !showPopup.value) {
      event.preventDefault()
      emit('search')
      return
    }

    // Handle backspace for atomic filter deletion
    if (event.key === 'Backspace') {
      const chipInfo = findFilterChipAtCursor()
      if (chipInfo) {
        event.preventDefault()
        const text = searchQuery.value
        const newText = text.slice(0, chipInfo.start) + text.slice(chipInfo.end)
        searchQuery.value = newText

        nextTick(() => {
          renderContent()
          setCursorOffset(chipInfo.start)
        })
        return
      }
    }

    // Handle autocomplete popup navigation
    if (showPopup.value) {
      switch (event.key) {
        case 'ArrowDown': {
          event.preventDefault()
          selectedIndex.value = Math.min(selectedIndex.value + 1, suggestions.value.length - 1)
          scrollSelectedIntoView()
          return
        }
        case 'ArrowUp': {
          event.preventDefault()
          selectedIndex.value = Math.max(selectedIndex.value - 1, 0)
          scrollSelectedIntoView()
          return
        }
        case 'Tab':
        case 'Enter': {
          const selectedSuggestion = suggestions.value[selectedIndex.value]
          if (selectedSuggestion && selectedSuggestion.type !== 'hint') {
            event.preventDefault()
            selectSuggestion(selectedSuggestion)
          }
          return
        }
        case 'Escape': {
          event.preventDefault()
          suggestions.value = []
          filterState.value = null
          return
        }
      }
    }
  }

  function scrollSelectedIntoView () {
    nextTick(() => {
      if (!popupRef.value) return
      const selectedEl = popupRef.value.querySelector('.filter-popup-item--selected')
      if (selectedEl) {
        selectedEl.scrollIntoView({ block: 'nearest' })
      }
    })
  }

  function selectSuggestion (suggestion: Suggestion) {
    if (!filterState.value) return

    const text = searchQuery.value
    const beforeTrigger = text.slice(0, filterState.value.triggerIndex)
    const afterPartial = text.slice(filterState.value.triggerIndex + 1)
    const spaceIndex = afterPartial.indexOf(' ')
    const hasSpace = spaceIndex !== -1
    const afterFilter = hasSpace ? afterPartial.slice(spaceIndex) : ''

    let newText = ''
    let cursorOffset = 0

    switch (suggestion.type) {
      case 'filter-key': {
        const def = getFilterDefinition(suggestion.value)
        if (def?.type === 'boolean') {
          // Boolean filters commit immediately with space
          newText = `${beforeTrigger}#${suggestion.value} ${afterFilter.trimStart()}`
          cursorOffset = beforeTrigger.length + suggestion.value.length + 2 // +2 for # and space
        } else {
          // Other filters need a colon
          newText = `${beforeTrigger}#${suggestion.value}:${afterFilter.trimStart()}`
          cursorOffset = beforeTrigger.length + suggestion.value.length + 2 // +2 for # and :
        }
        break
      }
      case 'operator': {
        // Insert operator after colon
        const colonIndex = afterPartial.indexOf(':')
        if (colonIndex !== -1) {
          const afterColon = afterPartial.slice(colonIndex + 1)
          newText = `${beforeTrigger}#${filterState.value.key}:${suggestion.value}${afterColon}${afterFilter}`
          cursorOffset = beforeTrigger.length + filterState.value.key.length + 2 + suggestion.value.length // +2 for # and :
        }
        break
      }
      case 'value': {
        // Replace current value
        const colonIndex = afterPartial.indexOf(':')
        if (colonIndex !== -1) {
          const afterColon = afterPartial.slice(colonIndex + 1)
          const spaceInValue = afterColon.indexOf(' ')
          const afterValue = spaceInValue === -1 ? '' : afterColon.slice(spaceInValue)
          newText = `${beforeTrigger}#${filterState.value.key}:${suggestion.value}${afterValue}${afterFilter}`
          cursorOffset = beforeTrigger.length + filterState.value.key.length + 2 + suggestion.value.length // +2 for # and :
        }
        break
      }
      case 'hint': {
        return // Hint suggestions are not selectable
      }
    }

    searchQuery.value = newText
    suggestions.value = []
    selectedIndex.value = 0
    filterState.value = null

    nextTick(() => {
      renderContent()
      setCursorOffset(cursorOffset)
      editableRef.value?.focus()
    })
  }

  function resolveFilterState () {
    if (!isFocused.value || !searchQuery.value) {
      suggestions.value = []
      filterState.value = null
      hasValidationError.value = false
      return
    }

    const text = searchQuery.value
    const cursorOffset = getCursorOffset() ?? text.length

    // Parse filter state at cursor position
    const state = parseFilterState(text, cursorOffset)

    if (!state) {
      suggestions.value = []
      filterState.value = null
      hasValidationError.value = false
      return
    }

    // Don't show popup if this filter position is already committed as a chip
    if (committedChipPositions.value.has(state.triggerIndex)) {
      suggestions.value = []
      filterState.value = null
      hasValidationError.value = false
      return
    }

    filterState.value = state
    const def = getFilterDefinition(state.key)

    // Generate suggestions
    const newSuggestions = generateSuggestions(state, def)
    suggestions.value = newSuggestions
    selectedIndex.value = 0

    // Check for validation errors
    if (def) {
      const error = getFilterValidationError(
        def,
        state.value,
        state.rangeMin,
        state.rangeMax,
      )
      hasValidationError.value = Boolean(error)
    } else {
      hasValidationError.value = false
    }

    nextTick(() => {
      updatePopupPosition()
    })
  }

  // Watch searchQuery for external changes and filter resolution
  watch(searchQuery, newVal => {
    resolveFilterState()

    // Only re-render if the value changed externally
    if (editableRef.value && getPlainText() !== newVal) {
      renderContent()
    }
  })

  // Initial render
  onMounted(() => {
    if (searchQuery.value) {
      renderContent()
    }
  })
</script>

<style scoped>
.search-autocomplete-wrapper {
  position: relative;
  display: flex;
  flex-grow: 1;
}

.search-input-container {
  display: flex;
  align-items: center;
  flex-grow: 1;
  min-height: 48px;
  padding: 0 12px;
  background: rgb(var(--v-theme-surface));
  border: 1px solid rgba(var(--v-theme-on-surface), 0.38);
  border-radius: 4px;
  transition: border-color 0.2s, box-shadow 0.2s;
  gap: 8px;
}

.search-input-container:hover {
  border-color: rgba(var(--v-theme-on-surface), 0.87);
}

.search-input-container.is-focused {
  border-color: rgb(var(--v-theme-primary));
  border-width: 2px;
  padding: 0 11px; /* Compensate for border width */
  box-shadow: none;
}

.search-input-container.has-error {
  border-color: rgb(var(--v-theme-error));
}

.search-icon {
  color: rgba(var(--v-theme-on-surface), 0.4);
  flex-shrink: 0;
}

.search-editable {
  flex-grow: 1;
  min-height: 24px;
  outline: none;
  font-size: 16px;
  line-height: 24px;
  word-wrap: break-word;
  white-space: pre-wrap;
  text-align: left;
  color: rgb(var(--v-theme-on-surface));
}

.search-editable:empty::before {
  content: attr(data-placeholder);
  color: rgba(var(--v-theme-on-surface), 0.4);
  pointer-events: none;
}

.clear-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 4px;
  border: none;
  background: none;
  cursor: pointer;
  color: rgba(var(--v-theme-on-surface), 0.4);
  border-radius: 50%;
  transition: background-color 0.2s;
  flex-shrink: 0;
}

.clear-btn:hover {
  background: rgba(var(--v-theme-on-surface), 0.04);
  color: rgba(var(--v-theme-on-surface), 0.6);
}
</style>

<style>
/* Filter chip styles - not scoped because they're inserted via innerHTML */
.filter-chip {
  display: inline-flex;
  align-items: center;
  background: rgb(var(--v-theme-filter-chip));
  color: rgb(var(--v-theme-filter-chip-text));
  padding: 1px 6px;
  border-radius: 4px;
  font-weight: 500;
  font-size: 14px;
  margin: 0 1px;
  user-select: all;
  cursor: default;
}

.filter-chip:hover {
  filter: brightness(0.95);
}

/* Filter popup styles */
.filter-popup {
  background: rgb(var(--v-theme-filter-popup-bg));
  border: 1px solid rgb(var(--v-theme-filter-popup-border));
  border-radius: 8px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
  min-width: 180px;
  max-width: 280px;
  max-height: 240px;
  overflow: hidden;
}

.filter-popup-list {
  max-height: 240px;
  overflow-y: auto;
  padding: 4px;
}

.filter-popup-item {
  padding: 8px 12px;
  cursor: pointer;
  border-radius: 4px;
  font-family: 'JetBrains Mono', 'Fira Code', monospace;
  font-size: 13px;
  color: rgb(var(--v-theme-filter-popup-item));
  transition: background-color 0.15s ease;
  display: flex;
  align-items: center;
  gap: 8px;
}

.filter-popup-item:hover {
  background: rgb(var(--v-theme-filter-popup-item-hover));
}

.filter-popup-item--selected {
  background: rgb(var(--v-theme-filter-popup-item-selected));
}

.filter-popup-item--selected:hover {
  background: rgb(var(--v-theme-filter-popup-item-selected));
}

.filter-popup-item--hint {
  cursor: default;
  opacity: 0.7;
  font-style: italic;
}

.filter-popup-item--hint:hover {
  background: transparent;
}

.filter-prefix {
  color: rgb(var(--v-theme-filter-popup-prefix));
  margin-right: 2px;
  font-weight: 600;
}

.filter-label {
  flex: 1;
}

.filter-description {
  font-size: 11px;
  opacity: 0.6;
  font-style: normal;
}

/* Custom scrollbar for the popup */
.filter-popup-list::-webkit-scrollbar {
  width: 6px;
}

.filter-popup-list::-webkit-scrollbar-track {
  background: transparent;
}

.filter-popup-list::-webkit-scrollbar-thumb {
  background: rgba(var(--v-theme-on-surface), 0.2);
  border-radius: 3px;
}

.filter-popup-list::-webkit-scrollbar-thumb:hover {
  background: rgba(var(--v-theme-on-surface), 0.3);
}
</style>
