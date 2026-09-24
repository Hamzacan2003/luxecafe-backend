import axios from 'axios';

const api = axios.create({
    baseURL: 'https://localhost:7243/api', // veya kullandığınız port örn: http://localhost:5000/api
});

// Giden her isteğe localStorage'daki token'ı ekle
api.interceptors.request.use(
    (config) => {
        const token = localStorage.getItem('token');
        if (token) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => {
        return Promise.reject(error);
    }
);

export default api;