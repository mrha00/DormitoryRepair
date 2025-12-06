import axios from 'axios'

const api = axios.create({
  baseURL: 'http://localhost:5002/api',
  timeout: 5000
})

export const login = (username, password) => {
  return api.post('/auth/login', { username, password })
}