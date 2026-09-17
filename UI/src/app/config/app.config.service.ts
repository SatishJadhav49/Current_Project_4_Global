import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AppConfig {
  public apiPort: String = '5000';

  // For Live
  // public apiPort: String = '2448';
  // public apiPort: String = '443';


  // Python AI service (defect summary)
  public aiApiPath: String = 'http://localhost:8000/service/';

  // For Live
  // public aiApiPath: String = 'http://<ai-server>:8000/service/';

  public apiProtocol?: String;
  public apiHostName?: String;
  public baseApiPath: String;
  public basePath: String;
  public locale?: String;

  constructor() {
    this.basePath = '';
    if (!this.apiProtocol) {
      this.apiProtocol = window.location.protocol;
    }
    if (!this.apiHostName) {
      this.apiHostName = window.location.hostname;
    }
    if (!this.apiPort) {
      this.apiPort = window.location.port;
    }
    this.baseApiPath =
      this.apiProtocol + '//' + this.apiHostName + ':' + this.apiPort + (this.apiPort=='443' ? '/PQLSP/api/' : '/api/');
    if (!this.locale) {
      this.locale = navigator.language;
    }
  }
}
