import type {OllamaModelOptions} from "./websocket-messaging.ts";

export interface SessionConfiguration {
    model: string
    system_prompt: string
    all_tools: OllamaToolDefinition[]
    options: OllamaModelOptions
}

export interface OllamaFunctionParameters {
    type: string
    properties: Record<string, any>
    required?: string[]
}

export interface OllamaFunctionDefinition {
    name: string
    description: string
    parameters: OllamaFunctionParameters
}

export interface OllamaToolDefinition {
    type: string
    function: OllamaFunctionDefinition
}

export interface IRestApi {
    deleteSession(sessionId: string): Promise<void>

    updateSessionConfiguration(sessionId: string, configuration: SessionConfiguration): Promise<SessionConfiguration>
}

class RestApi implements IRestApi {

    constructor(private readonly _baseUrl: string) {
    }

    async updateSessionConfiguration(sessionId: string, configuration: SessionConfiguration): Promise<SessionConfiguration> {
        const response = await fetch(`${this._baseUrl}/api/v1/sessions/${sessionId}/configuration`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(configuration)
        })
        if (!response.ok) throw new Error(
            `Failed to update session configuration: ${response.status} ${response.statusText}`
        )
        return response.json()
    }

    async deleteSession(sessionId: string): Promise<void> {
        const response = await fetch(`${this._baseUrl}/api/v1/sessions/${sessionId}`, {
            method: 'DELETE',
        })
        if (!response.ok) throw new Error(
            `Failed to delete session: ${response.status} ${response.statusText}`
        )
        return response.json()
    }


}


export const createRestApi = (baseUrl: string) => new RestApi(baseUrl)