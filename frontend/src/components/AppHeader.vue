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
        <span v-if="!isMobile" class="title-text">Japanese Dictionary</span>
      </div>

    </v-app-bar-title>

    <!-- Desktop navigation -->
    <template v-if="!isMobile">
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
      <v-btn
        icon
        @click="changeTheme"
      >
        <v-icon>mdi-theme-light-dark</v-icon>
      </v-btn>
    </template>

    <!-- Mobile: Icon-only search button -->
    <v-btn
      v-if="isMobile"
      active-color="primary"
      icon
      :class="{ 'mr-2': isMobile }"
      to="/search"
    >
      <v-icon>mdi-magnify</v-icon>
    </v-btn>

    <!-- Mobile burger menu (right side) -->
    <v-app-bar-nav-icon
      v-if="isMobile"
      variant="plain"
      @click="drawer = !drawer"
    />
  </v-app-bar>

  <!-- Mobile navigation drawer -->
  <v-navigation-drawer
    v-model="drawer"
    location="right"
    color="background"
    temporary
  >
    <v-list
      color="primary"
    >
      <v-list-item
        prepend-icon="mdi-magnify"
        title="Search"
        to="/search"
        @click="drawer = false"
      />
      <v-list-item
        prepend-icon="mdi-information"
        title="About"
        to="/about"
        @click="drawer = false"
      />
      <v-divider class="my-2" />
      <v-list-item
        prepend-icon="mdi-theme-light-dark"
        title="Toggle Theme"
        @click="changeTheme"
      />
    </v-list>
  </v-navigation-drawer>
</template>
<script setup>
  import { ref, computed } from 'vue'
  import { useTheme } from 'vuetify'
  import { useDisplay } from 'vuetify'

  const theme = useTheme()
  const { xs, sm } = useDisplay()
  const isMobile = computed(() => xs.value || sm.value)
  const drawer = ref(false)

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
