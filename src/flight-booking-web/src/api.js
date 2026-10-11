const apiBase = (import.meta.env.VITE_API_BASE_URL || '/api').replace(/\/+$/, '');
let token = '';
export function setToken(value) { token = value; }
export async function api(path, { method = 'GET', body, headers = {} } = {}) {
  const response = await fetch(`${apiBase}${path}`, { method, headers: { ...(body ? { 'Content-Type': 'application/json' } : {}), ...(token ? { Authorization: `Bearer ${token}` } : {}), ...headers }, ...(body ? { body: JSON.stringify(body) } : {}) });
  if (response.status === 204) return null;
  const text = await response.text();
  let result;
  try { result = text ? JSON.parse(text) : {}; } catch { result = {}; }
  if (!response.ok) {
    const details = result.errors ? Object.values(result.errors).flat().join(' ') : result.detail || result.title;
    throw new Error(response.status === 401 ? 'Please sign in again. Your session may have expired.' : response.status === 403 ? 'Your account cannot access this feature.' : details || `Request failed (${response.status}). Check the backend and supplier services.`);
  }
  return result;
}
