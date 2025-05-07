import * as axios from "axios";
import { Loadable, Loading, ProblemDetails } from "Loading";

/**
 * Base api wrapper used for all other api classes
 */
export default class ApiWrapper<T> {

	/**
	 * Read a list of paramtype T from a URL
	 * 
	 * @param url		URL to perform the GET request on
	 * @param reader	Reader to transform the entires into the type
	 */
	public async readList<U>(url: string, reader: ApiReader<U>): Promise<Loading<U[]>> {
		const data: Loading<any> = await this.getData(url);
		if (data.state != "loaded") {
			return data;
		}

		if (Array.isArray(data.data) == false) {
			return Loadable.error(`expected array for readList. is readSingle supposed to be used instead? URL: '${url}'`);
		}

		const arr: U[] = data.data.map((iter: any) => {
			return reader(iter);
		});

		return Loadable.loaded(arr);
	}

	/**
	 * Read a single paramtype T from a URL
	 * 
	 * @param url		URL to perform the GET request on
	 * @param reader	Reader to transform the entires into the type
	 */
	public async readSingle<U>(url: string, reader: ApiReader<U>): Promise<Loading<U>> {
		const data: Loading<any> = await this.getData(url);
		if (data.state != "loaded") {
			return data;
		}

		if (Array.isArray(data.data) == true) {
			return Loadable.error(`unexpected array for readSingle. is readList supposed to be used instead? URL: '${url}'`);
		}

		const datum: U = reader(data.data);
		return Loadable.loaded(datum);
	}

	public async delete(url: string): Promise<Loading<void>> {
		const response: axios.AxiosResponse<any> = await axios.default.delete(url, { validateStatus: () => true });

		return this._getLoading<void>(response, url) ?? Loadable.loaded(undefined);
    }

	public async post(url: string): Promise<Loading<void>> {
		return this.postForm(url, undefined);
	}

	public async postForm(url: string, body: any, extraConfig?: axios.AxiosRequestConfig): Promise<Loading<void>> {
        const response: axios.AxiosResponse<any> = await axios.default.post(url, body, {
            validateStatus: () => true,
            maxRedirects: 0,
            ...extraConfig
        });

		return this._getLoading<void>(response, url) ?? Loadable.loaded(undefined);
    }


	public async postReply<U>(url: string, reader: ApiReader<U>): Promise<Loading<U>> {
		return this.postReplyForm(url, undefined, reader);
	}

	public async postReplyForm<U>(url: string, body: any, reader: ApiReader<U>, extraConfig?: axios.AxiosRequestConfig): Promise<Loading<U>> {
        const response: axios.AxiosResponse<any> = await axios.default.post(url, body, {
            validateStatus: () => true,
            maxRedirects: 0,
            ...extraConfig
        });

        const l = this._getLoading<U>(response, url);
        if (l != null) {
            return l;
        }

        const datum: U = reader(response.data);
        return Loadable.loaded(datum);
    }

	/**
	 * Common method to call axios to get some data, then handle the status and return an appropriate Loading object
	 */
	private async getData(url: string): Promise<Loading<any>> {
		const response: axios.AxiosResponse<any> = await axios.default.get(url, { validateStatus: () => true });

        const l = this._getLoading<void>(response, url);
        if (l != null) {
            return l;
        }

		return Loadable.loaded(response.data);
	}

	private _getLoading<T>(response: axios.AxiosResponse<any>, url: string): Loading<T> | null {
		if (response.status == 204) {
			return Loadable.nocontent();
		} else if (response.status == 400) {
			return Loadable.error(response.data, url);
		} else if (response.status == 403) {
			return Loadable.error(`forbidden: you are not signed in, or your account lacks permissions`, url);
		} else if (response.status == 404) {
			return Loadable.notFound(response.data);
		} else if (response.status == 405) {
			return Loadable.error(`method not allowed`, url);
        } else if (response.status == 429) {
            return Loadable.error(`you have been rate limited! more info: ${response.data}`, url);
        } else if (response.status == 500) {
            return Loadable.error(response.data, url);
        } else if (response.status == 524 || response.status == 504) {
            return Loadable.error(`timeout from cloudflare`, url);
        }

		if (response.status != 200) {
			throw `unchecked status code ${response.status}: ${response.data}`;
		}

		return null;
	}


}

export type ApiReader<T> = (elem: any) => T;
