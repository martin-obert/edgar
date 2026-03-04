import {createRouter, createWebHistory} from "vue-router";
import HomeView from "./views/HomeView.vue";
import GameView from "./views/GameView.vue";

const routes = [
    { path: '/', component: HomeView },
    { path: '/game/:sessionId', component: GameView, props: true },
]

export default createRouter({
    history: createWebHistory(),
    routes,
})