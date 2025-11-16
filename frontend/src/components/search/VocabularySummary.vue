<template>
    <v-card
        class="pa-4 mb-4"
        outlined
    >
        <v-row>
            <v-col cols="4">
                <div class="vocabulary-primary">
                    <span class="furigana">
                        {{  vocabulary.primaryKanji ? vocabulary.primaryKana?.text : '' }}
                    </span>
                    <span>
                        {{ vocabulary.primaryKanji?.text ?? vocabulary.primaryKana?.text }}
                    </span>
                    <div>
                        <v-chip
                            v-if="vocabulary.isCommon"
                            size="small"
                            color="primary"
                            text-color="white"
                            class="mt-1"
                            style="width: fit-content;"
                        >
                            {{ vocabulary.isCommon ? 'Common' : '' }}
                        </v-chip>
                        <v-chip
                            v-if="vocabulary.jlptLevel"
                            size="small"
                            color="primary"
                            text-color="white"
                            class="mt-1"
                            style="width: fit-content;"
                        >
                            JLPT N{{ vocabulary.jlptLevel }}
                        </v-chip>
                        <v-chip
                            v-for="tag in tags"
                            size="small"
                            color="primary"
                            text-color="white"
                            class="mt-1"
                            style="width: fit-content;"
                        >
                            {{ tag.description }}
                        </v-chip>
                    </div>
                </div>
            </v-col>
            <v-col cols="8" class="text-left">
                <div class="meanings">
                    <div
                        v-for="(sense, index) in vocabulary.senses"
                        :key="index"
                    >
                        {{ index + 1 }}. {{ getGlossaryText(sense) }}
                    </div>
                </div>
            </v-col>
        </v-row>
        <div class="vocabulary-primary"></div>
    </v-card>
</template>
<script lang="ts" setup>
import type { VocabularySummary } from '@/types/Vocabulary';

const props = defineProps<{
    vocabulary: VocabularySummary;
}>();

const tags = computed(() => {
    return [
        ...props.vocabulary.primaryKanji?.tags || [],
        ...props.vocabulary.primaryKana?.tags || [],
        ...props.vocabulary.otherKanjiForms?.flatMap(kanji => kanji.tags) || [],
        ...props.vocabulary.otherKanaForms?.flatMap(kana => kana.tags) || []
    ];
});

const getGlossaryText = (sense: any) => {
    return sense.glosses.map((gloss: any) => gloss.text).join(', ');
};

</script>
<style lang="scss" scoped>
.vocabulary-primary {
    display: flex;
    flex-direction: column;
    font-size: 1.3rem;
    font-weight: 500;
    text-align: left;
    justify-content: left;

    .furigana {
        font-size: 0.8rem;
        color: gray;
    }

    v-chip  {
        &__content {
            font-size: 0.5rem;
        }
    }
}
</style>