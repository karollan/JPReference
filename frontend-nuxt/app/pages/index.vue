<template>
  <v-container
    class="text-center home"
  >
    <v-row
      class="home__content"
      :style="{
        marginTop: mdAndDown ? '0' : '-6rem'
      }"
      dense
    >
      <v-col cols="12">
        <h1
          class="home__title"
          :class="{ 'home__title--mobile': mdAndDown }"
        >
          JP Reference
        </h1>
      </v-col>
      <v-col cols="12">
        <h5
          class="home__subtitle"
          :class="{ 'home__subtitle--mobile': mdAndDown }"
        >
          Your essential dictionary for Japanese vocabulary and kanji. Start searching to accelerate your learning
        </h5>
      </v-col>
      <v-col cols="12">
        <SearchAutocomplete
          v-model:search-query="searchQuery"
          placeholder="Search for vocabulary or kanji"
          :show-radical-search="false"
          @clear="searchQuery = ''"
          @search="handleSearch"
        />
      </v-col>
      <v-col
        class="home__actions"
        cols="12"
      >
        <v-btn
          class="text-none"
          color="primary"
          :disabled="disabled"
          elevation="0"
          @click="handleSearch"
        >
          Explore
        </v-btn>
      </v-col>
    </v-row>
  </v-container>
</template>

<script lang="ts" setup>
  import { useDisplay } from 'vuetify'

  const { mdAndDown } = useDisplay()

  const router = useRouter()
  const searchQuery = ref('')

  const disabled = computed(() => searchQuery.value.trim().length === 0)
  function handleSearch () {
    if (disabled.value) return
    router.push({
      path: '/search',
      query: {
        query: searchQuery.value
      }
    })
  }

  useHead({
    title: 'Japanese Dictionary - JP Reference',
    meta: [
      {
        name: 'description',
        content: 'A comprehensive Japanese dictionary for Japanese students. Search for vocabulary, kanji, proper nouns, and radicals.'
      }
    ]
  })
</script>
<style lang="scss" scoped>
.home {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  max-width: unset;

  &__content {
    max-width: 42rem;
  }

  &__title {
    font-size:3.5rem;
    margin-bottom: 1rem;

    &--mobile {
      font-size:2.5rem;
      margin-bottom: 0.5rem;
    }
  }

  &__subtitle {
    font-size:1.125rem;
    margin-bottom: 2rem;
    font-weight: normal;

    &--mobile {
      font-size:1rem;
      margin-bottom: 0.5rem;
    }
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
