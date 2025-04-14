class HttpClient {
    constructor(baseUrl) {
        this.baseUrl = baseUrl;
    }

    async get(url) {
        const response = await fetch(this.baseUrl + url);
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return await response.json();
    }

    async post(url, data) {
        const response = await fetch(this.baseUrl + url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return await response.json();
    }
}