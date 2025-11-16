<template>
    <v-card
        class="pa-4 mb-4"
        outlined
    >
        <div class="kanji-literal">{{ kanji.literal }}</div>
    </v-card>
</template>
<script lang="ts" setup>
import type { KanjiSummary } from '@/types/Kanji';


defineProps<{
    kanji: KanjiSummary;
}>();

</script>
<style lang="scss" scoped>
</style>