import {
  HttpClient,
  RequestConfig,
  ApiResponse,
  EasyAuthConfig
} from '../types';

/**
 * Default HTTP client implementation using fetch API
 */
export class DefaultHttpClient implements HttpClient {
  private readonly baseUrl: string;
  private readonly defaultTimeout: number;
  
  constructor(config: EasyAuthConfig) {
    this.baseUrl = config.baseUrl.replace(/\/$/, ''); // Remove trailing slash
    this.defaultTimeout = config.timeout ?? 30000;
  }
  
  async get<T>(url: string, config?: RequestConfig): Promise<ApiResponse<T>> {
    return this.request<T>('GET', url, undefined, config);
  }
  
  async post<T>(url: string, data?: unknown, config?: RequestConfig): Promise<ApiResponse<T>> {
    return this.request<T>('POST', url, data, config);
  }
  
  async put<T>(url: string, data?: unknown, config?: RequestConfig): Promise<ApiResponse<T>> {
    return this.request<T>('PUT', url, data, config);
  }
  
  async delete<T>(url: string, config?: RequestConfig): Promise<ApiResponse<T>> {
    return this.request<T>('DELETE', url, undefined, config);
  }
  
  private async request<T>(
    method: string,
    url: string,
    data?: unknown,
    config?: RequestConfig
  ): Promise<ApiResponse<T>> {
    const fullUrl = url.startsWith('http') ? url : `${this.baseUrl}${url}`;
    const timeout = config?.timeout ?? this.defaultTimeout;
    
    const headers = {
      'Content-Type': 'application/json',
      ...config?.headers
    };
    
    const abortController = new AbortController();
    const timeoutId = setTimeout(() => abortController.abort(), timeout);
    
    try {
      const fetchOptions: RequestInit = {
        method,
        headers,
        credentials: config?.withCredentials ? 'include' : 'same-origin',
        signal: abortController.signal
      };

      if (data) {
        fetchOptions.body = JSON.stringify(data);
      }

      const response = await fetch(fullUrl, fetchOptions);
      
      clearTimeout(timeoutId);
      
      const responseData = await this.parseResponse<T>(response);
      
      return {
        data: responseData,
        status: response.status,
        statusText: response.statusText,
        headers: this.parseHeaders(response.headers)
      };
    } catch (error) {
      clearTimeout(timeoutId);
      
      if (error instanceof Error && error.name === 'AbortError') {
        throw new Error(`Request timeout after ${timeout}ms`);
      }
      
      throw error;
    }
  }
  
  private async parseResponse<T>(response: Response): Promise<T> {
    const contentType = response.headers.get('content-type');
    
    if (contentType?.includes('application/json')) {
      return response.json() as Promise<T>;
    }
    
    return response.text() as Promise<T>;
  }
  
  private parseHeaders(headers: Headers): Record<string, string> {
    const result: Record<string, string> = {};
    headers.forEach((value, key) => {
      result[key] = value;
    });
    return result;
  }
}