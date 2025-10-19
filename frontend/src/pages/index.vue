<template>
  <v-container
    class="text-center home"
  >
    <v-row justify="center" dense>
      <v-col cols="12">
        <h1
          class="home__title"
        >
          Master the JLPT
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
        <v-autocomplete
          bg-color="white"
          icon-color="#00000066"
          class="home__search mx-auto"
          density="comfortable"
          placeholder="Search for vocabulary or kanji"
          prepend-inner-icon="mdi-magnify"
          variant="outlined"
        >
        </v-autocomplete>
      </v-col>
      <v-col
        cols="12"
        class="home__actions"  
      >
        <v-btn
          elevation="0"
          class="text-none"
        >
          Explore kanji
        </v-btn>
        <v-btn
          elevation="0"
          class="text-none"
        >
          Explore vocabulary
        </v-btn>
      </v-col>
    </v-row>
  </v-container>
</template>

<script lang="ts" setup>
  //
</script>
<style lang="scss" scoped>
.home {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  max-width: 42rem;

  &__title {
    font-size:3.75rem;
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
