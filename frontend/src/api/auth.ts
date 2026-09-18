import client from './client'

export interface AuthResponse {
  token: string
  username: string
  email: string
}

export async function register(username: string, email: string, password: string): Promise<AuthResponse> {
  const { data } = await client.post<AuthResponse>('/auth/register', { username, email, password })
  return data
}

export async function login(username: string, password: string): Promise<AuthResponse> {
  const { data } = await client.post<AuthResponse>('/auth/login', { username, password })
  return data
}
