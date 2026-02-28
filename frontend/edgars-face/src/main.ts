import {createApp} from 'vue'
import './style.css'
import App from './App.vue'
import PrimeVue from 'primevue/config';
import Aura from '@primeuix/themes/aura';
import {createPinia} from "pinia";
import routing from "./routing";

const pinia = createPinia()

createApp(App)
    .use(pinia)
    .use(routing)
    .use(PrimeVue, {
        theme: {
            preset: Aura
        }
    })
    .mount('#app')
