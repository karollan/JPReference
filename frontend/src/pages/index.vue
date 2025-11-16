<template>
  <v-container
    class="text-center home"
  >
    <v-row justify="center" dense>
      <v-col cols="12">
        <h1
          class="home__title"
        >
          Japanese Dictionary
        </h1>
      </v-col>
      <v-col cols="12">
        <h5
          class="home__subtitle"
        >
          Your essential resource for Japanese vocabulary and kanji. Start searching to accelerate your learning
        </h5>
      </v-col>
      <v-col cols="12">
        <v-text-field
          v-model="searchQuery"
          bg-color="white"
          icon-color="#00000066"
          class="home__search mx-auto"
          density="comfortable"
          placeholder="Search for vocabulary or kanji"
          prepend-inner-icon="mdi-magnify"
          variant="outlined"
        >
        </v-text-field>
      </v-col>
      <v-col
        cols="12"
        class="home__actions"  
      >
        <v-btn
          elevation="0"
          class="text-none"
          color="primary"
          :disabled="disabled"
          :to="`/search/?query=${encodeURIComponent(searchQuery)}`"
        >
          Explore
        </v-btn>
      </v-col>
    </v-row>
  </v-container>
</template>

<script lang="ts" setup>
const searchQuery = ref('');

const disabled = computed(() => searchQuery.value.trim().length === 0);
</script>
<style lang="scss" scoped>
.home {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  max-width: 42rem;

  &__title {
    font-size:3.5rem;
    margin-bottom: 1.5rem;
  }

  &__subtitle {
    font-size:1.125rem;
    margin-bottom: 2.5rem;
    font-weight: normal;
  }

  &__actions {
    display: flex;
    gap: 1rem;

    & > .v-btn {
      flex: 1 1 0%;
      height: unset;
      border-radius: 0.5rem;
      padding: 0.75rem 1.5rem;
    }
  }
}

</style>
