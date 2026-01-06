<template>
  <v-app-bar
    border="b-sm"
    color="background"
    elevation="0"
  >
    <v-app-bar-title class="d-flex align-center">
      <div class="d-flex align-center">
        <RouterLink to="/">
          <v-icon
            color="primary"
            icon="mdi-ideogram-cjk-variant"
            size="large"
            style="cursor: pointer; text-decoration:none; border-radius: 50%;"
            to="/"
          />
        </RouterLink>
        <span class="title-text">Japanese Dictionary</span>
      </div>

    </v-app-bar-title>
    <v-btn
      active-color="primary"
      class="text-none"
      text="Search"
      to="/search"
    />
    <v-btn
      active-color="primary"
      class="text-none"
      text="About"
      to="/about"
    />
    <!-- <v-btn
      active-color="primary"
      class="text-none"
      text="API"
      to="/api"
    /> -->
    <v-btn
      icon
      @click="changeTheme"
    >
      <v-icon>mdi-theme-light-dark</v-icon>
    </v-btn>
  </v-app-bar>
</template>
<script setup>
  import { useTheme } from 'vuetify'

  const theme = useTheme()

  function changeTheme () {
    theme.cycle([
      'jlptTheme',
      'jlptThemeDark',
    ])
  }

</script>
<style lang="scss" scoped>
a.v-btn::hover {
    background-color: unset;
}
a.v-btn--active > .v-btn__overlay {
    opacity: 0;
}
.title-text {
    font-size: 1.3rem;
    font-weight: 500;
    margin-left: 0.5rem;
}
</style>
