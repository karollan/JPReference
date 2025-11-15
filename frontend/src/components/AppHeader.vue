<template>
    <v-app-bar
        elevation="0"
        border="b-sm"
        color="background"
    >
        <v-app-bar-title class="d-flex align-center">
            <div class="d-flex align-center">
                <RouterLink to="/">
                    <v-icon
                        icon="mdi-ideogram-cjk-variant"
                        size="large"
                        color="primary"
                        style="cursor: pointer; text-decoration:none; border-radius: 50%;"
                        to="/"
                    >
                    </v-icon>
                </RouterLink>
                <span class="title-text">Japanese Dictionary</span>
            </div>

        </v-app-bar-title>
        <v-btn
            to="/search"
            active-color="primary"
            text="Search"
            class="text-none"
        >
        </v-btn>
        <v-btn
            to="/about"
            active-color="primary"
            text="About"
            class="text-none"
        >
        </v-btn>
        <v-btn
            to="/api"
            active-color="primary"
            text="API"
            class="text-none"
        >
        </v-btn>
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

const changeTheme = () => {
    theme.cycle([
        'jlptTheme',
        'jlptThemeDark'
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