export interface SessionConfiguration {
    model: string
    system_prompt: string
}

export interface IRestApi {

    updateSessionConfiguration(sessionId: string, configuration: SessionConfiguration): Promise<SessionConfiguration>

    getSessionConfiguration(sessionId: string): Promise<SessionConfiguration>
}

class RestApi implements IRestApi {

    constructor(private readonly _baseUrl: string) {

    }

    async getSessionConfiguration(sessionId: string): Promise<SessionConfiguration> {
        const response = await fetch(`${this._baseUrl}/api/v1/sessions/${sessionId}/configuration`, {
            method: 'GET',
        })

        return response.json()
    }

    async updateSessionConfiguration(sessionId: string, configuration: SessionConfiguration): Promise<SessionConfiguration> {
        const response = await fetch(`${this._baseUrl}/api/v1/sessions/${sessionId}/configuration`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(configuration)
        })

        return response.json()
    }


}


export const createRestApi = (baseUrl: string) => new RestApi(baseUrl)