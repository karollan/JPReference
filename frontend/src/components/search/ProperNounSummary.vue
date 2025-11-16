<template>
    <v-card
        class="pa-4 mb-4"
        outlined
    >
        <div class="proper-noun-literal">{{ properNoun.primaryKanji?.text ?? properNoun.primaryKana?.text }}</div>
    </v-card>
</template>
<script lang="ts" setup>
import type { ProperNounSummary } from '@/types/ProperNoun';

defineProps<{
    properNoun: ProperNounSummary;
}>();
</script>
<style lang="scss" scoped>

</style>