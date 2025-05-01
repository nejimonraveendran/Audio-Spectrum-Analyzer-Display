class DisplayConfig  {
    constructor(parentElement, helpers, httpClient, displayType) {
        this._httpClient = httpClient;
        this._displayType = displayType;
        this._helpers = helpers;

        //configurable properties
        this._rows = 0;                
        this._cols = 6;
        this._brightness = 0;
        this._transitionSpeed = 0;
        this._peakWait = 0;
        this._peakWaitCountDown = 0;
        this._amplificationFactor = 0;
        this._showPeaks = true;
        this._showPeaksWhenSilent = false;
        this._isBrightnessSupported = false;
        this._pixelColors = [];
        this._peakColor = this._helpers.rgbToHexString(255, 255, 255); 

        //non-configurable properties
        this._parentElement = parentElement;
        this._winWid = window.innerWidth;
        this._winHgt = window.innerHeight;
        this._tblMatrixId = 'tblMatrix';
        this._gradientTopId = 'gradientTop';
        this._gradientBottomId = 'gradientBottom';
        this._btnDeployId = 'btnDeploy';
        this._peakColorId = 'peakColor';
        this._sldPeakWaitId = 'peakWait';
        this._sldPeakFalldownId = 'sldPeakFalldown';
        this._sldTransitionSpeedId = 'sldTransitionSpeed';
        this._sldAmplificationId = 'sldAmplification';
        this._sldBrightnessId = 'sldBrightness';
        this._chkShowPeakId = 'chkShowPeakId';
        this._chkShowPeaksWhenSilent = 'chkShowPeaksWhenSilent';
        this._blackPixel = 'black'; 
        this._pixelWid = this._winWid / (this._cols * 2);
        this._pixelHgt =  this._pixelWid;
        this._gradient = [];   
        this._configUrl = URL.apiServer + '?displayType=' + this._displayType; 
        this._html = '';    
    }

    loadConfig() {
        this._httpClient.get(this._configUrl)
            .then((config) => {                
                this._config = config;
                this._rows = config.Rows;
                this._cols = config.Cols;
                this._brightnessMin = config.BrightnessMin;
                this._brightness = config.Brightness;
                this._brightnessMax = config.BrightnessMax;
                this._transitionSpeedMin = config.TransitionSpeedMin;
                this._transitionSpeed = config.TransitionSpeed;
                this._transitionSpeedMax = config.TransitionSpeedMax;
                this._peakWaitMin = config.PeakWaitMin;
                this._peakWait = config.PeakWait;
                this._peakWaitMax = config.PeakWaitMax;
                this._peakWaitCountDownMin = config.PeakWaitCountDownMin;
                this._peakWaitCountDown = config.PeakWaitCountDown;
                this._peakWaitCountDownMax = config.PeakWaitCountDownMax;
                this._amplificationFactorMin = config.AmplificationFactorMin;
                this._amplificationFactor = config.AmplificationFactor;
                this._amplificationFactorMax = config.AmplificationFactorMax;
                this._showPeaks = config.ShowPeaks;
                this._showPeaksWhenSilent = config.ShowPeaksWhenSilent;
                this._isBrightnessSupported = config.IsBrightnessSupported;
                this._peakColor = this._helpers.rgbToHexString(config.PeakColor.R, config.PeakColor.G, config.PeakColor.B); 
                this._pixelColors = config.PixelColors;
                this._gradientStartColor = config.GradientStartColor;
                this._gradientEndColor = config.GradientEndColor;
                this._pixelWid = this._winWid / (this._cols * 2);
                this._pixelHgt =  this._pixelWid;
                
                this.buildConfigUi();

            })
            .catch((error) => {
                console.error('Error loading configuration:', error);
            });
    }

    saveConfig(newConfig) {
        this._httpClient.post(this._configUrl, newConfig)
            .then((result) => {
                if(result.DisplayType == DISPLAY_TYPE.WEB){
                    window.location = "/";  //redirect to home page after config udpate
                }
            })
            .catch((error) => {
                console.error('Error updating configuration:', error);
            });
    }

    buildConfigUi(){
        //build ui controls dynamically
        this._html = `<div id="configContainer" style="width: 100%; display: flex; flex-direction: column; align-items: center; padding: 10px;">`;
        this._html = this.buildSliders();
        this._html = this.buildToggles();
        this._html = this.builPeakColor();
        this._html = this.buildMatrix();
        this._html = this.buildDeployButton();
        this._html += `</div>`;

        //add to dom
        $(this._parentElement).html(this._html); 

        //bind events
        $(`#${this._gradientTopId}`).on('change', this.setGradient.bind(this));
        $(`#${this._gradientBottomId}`).on('change', this.setGradient.bind(this));
        $(`#${this._btnDeployId}`).on('click', this.deploy.bind(this));
        this.saveConfig.bind(this);                   
    }

    //deploy function - called by deploy button
    deploy(){
        let payload = {};

        //get slider values
        payload.PeakWait = parseInt($(`#${this._sldPeakWaitId}`).val());
        payload.Brightness = parseInt($(`#${this._sldBrightnessId}`).val());
        payload.TransitionSpeed = parseInt($(`#${this._sldTransitionSpeedId}`).val());
        payload.PeakWaitCountDown = parseInt($(`#${this._sldPeakFalldownId}`).val());
        payload.AmplificationFactor = parseInt($(`#${this._sldAmplificationId}`).val());

        //get checkbox values
        payload.ShowPeaks = $(`#${this._chkShowPeakId}`).is(':checked');
        payload.ShowPeaksWhenSilent = $(`#${this._chkShowPeaksWhenSilent}`).is(':checked');

        //get peak color
        let peakPixelColor = this._helpers.hexStringToRgbJson($(`#${this._peakColorId}`).val());
        payload.PeakColor = {R: peakPixelColor.r, G: peakPixelColor.g, B: peakPixelColor.b};
        
        //get pixels
        let pixelColors = [];
        for (let x = 0; x < this._cols; x++) {
            let columnPixels = [];
            for (let y = 0; y < this._rows; y++) {
                let pixel = $(`#${this._tblMatrixId} tbody tr td div input[data-px-x=${x}][data-px-y=${y}].pixel`); 
                let pixelRgbJson = this._helpers.hexStringToRgbJson($(pixel).val());
                columnPixels.push({R: pixelRgbJson.r, G: pixelRgbJson.g, B: pixelRgbJson.b});
            }
            pixelColors.push(columnPixels);
        }
        payload.PixelColors = pixelColors;

        //get gradient value (start)
        let gradientStartColor = this._helpers.hexStringToRgbJson($(`#${this._gradientBottomId}`).val());
        payload.GradientStartColor = {R: gradientStartColor.r, G: gradientStartColor.g, B: gradientStartColor.b};
        
        //get gradient value (end)
        let gradientEndColor = this._helpers.hexStringToRgbJson($(`#${this._gradientTopId}`).val());
        payload.GradientEndColor = {R: gradientEndColor.r, G: gradientEndColor.g, B: gradientEndColor.b};
        
        //send to server
        this.saveConfig(payload);
    }


    setGradient() {
        let topColor = $(`#${this._gradientTopId}`).val();
        let bottomColor = $(`#${this._gradientBottomId}`).val();
        this._gradient = this._helpers.generateGradient(bottomColor, topColor, this._rows);
        
        for (let x = 0; x < this._cols; x++) {
            for (let y = 0; y < this._rows; y++) {
                let pixel = $(`#${this._tblMatrixId} tbody tr td div input[data-px-x=${x}][data-px-y=${y}].pixel`); 
                $(pixel).val(this._gradient[y]);
            }
        }
    }

    buildSliders(){
        this._html += `<div id="slidersContainer" style="width: 100%; display: flex; flex-direction: column; align-items: start; padding: 10px;">`;
        this._html += `<label>Peak Wait</label>`;
        this._html += `<input id="${this._sldPeakWaitId}" type="range" min="${this._peakWaitMin}" max="${this._peakWaitMax}" step="1" value="${this._peakWait}" style="width:100%;margin-bottom:20px;"/>`;
        
        this._html += `<label>Peak Fall Interval</label>`;
        this._html += `<input id="${this._sldPeakFalldownId}" type="range" min="${this._peakWaitCountDownMin}" max="${this._peakWaitCountDownMax}" step="1" value="${this._peakWaitCountDown}" style="width:100%;margin-bottom:20px;"/>`;
        
        this._html += `<label>Transition Energy</label>`;
        this._html += `<input id="${this._sldTransitionSpeedId}" type="range" min="${this._transitionSpeedMin}" max="${this._transitionSpeedMax}" step="1" value="${this._transitionSpeed}" style="width:100%;margin-bottom:20px;"/>`;
        
        this._html += `<label>Level Amplification</label>`;
        this._html += `<input id="${this._sldAmplificationId}" type="range" min="${this._amplificationFactorMin}" max="${this._amplificationFactorMax}" step="1" value="${this._amplificationFactor}" style="width:100%;margin-bottom:20px;"/>`;

        if(this._isBrightnessSupported){
            this._html += `<label>Brightness</label>`;
            this._html += `<input id="${this._sldBrightnessId}" type="range" min="${this._brightnessMin}" max="${this._brightnessMax}" step="1" value="${this._brightness}" style="width:100%;margin-bottom:20px;"/>`;
        }
        else {
            this._html += `<input id="${this._sldBrightnessId}" type="range" min="0" max="0" step="0" value="0" style="width:100%;margin-bottom:20px;display:none;" disabled/>`;
        }

        this._html += `</div>`;
        return this._html;
    }

    buildToggles(){
        this._html += `<div id="togglesContainer" style="width: 100%; display: flex; flex-direction: row; align-items: start; padding-left: 20px;">`;
        this._html += `<input id="${this._chkShowPeakId}" type="checkbox" ${this._showPeaks ? "checked" : ""}/>`;
        this._html += `<label>Show peaks</label>`;

        this._html += `<input id="${this._chkShowPeaksWhenSilent}" type="checkbox" style="margin-left: 50px;" ${this._showPeaksWhenSilent ? "checked" : ""}/>`;
        this._html += `<label>Show bttom peaks when silent</label>`;

        this._html += `</div>`;
        return this._html;
    }


    builPeakColor() {
        this._html += `<div id="peakColorContainer" style="width: 100%; display: flex; flex-direction: column; align-items: center; padding:10px;">`;
        this._html += `<label>Peak Color</label>`;
        this._html += `<div style="width:${this._pixelWid}px;height:${this._pixelHgt}px;overflow:hidden;border-radius:20%;">`;
        this._html += `<input type="color" id="${this._peakColorId}" value="${this._peakColor}" style="cursor:pointer;width:${this._pixelWid * 3}px; height:${this._pixelHgt * 3}px; transform:translate(-50%, -50%);"/>`;
        this._html += `</div>`;
        this._html += `</div>`;
        return this._html;
    }

    buildMatrix() {
        let gradientPickerHeight = (this._pixelHgt * this._rows) / 2;
        
        this._html += `<div id="peakColorContainer" style="width: 100%; display: flex; flex-direction: column; align-items: center; padding:10px;">`;
        this._html += `<label>Pixel Colors</label>`;
        this._html += `<table id="${this._tblMatrixId}">`;
        this._html += `<tbody>`;

        let gradientStartColorHexString = this._helpers.rgbToHexString(this._gradientStartColor.R, this._gradientStartColor.G, this._gradientStartColor.B);
        let gradientEndColorHexString = this._helpers.rgbToHexString(this._gradientEndColor.R, this._gradientEndColor.G, this._gradientEndColor.B);
        
        for (let y = this._rows-1; y >= 0; y--) {  
            this._html += '<tr>';
            
            //build gradient color pickers
            if (y == this._rows-1) { 
                this._html += `<td rowspan="${this._rows}">`;
                this._html += `<p><i class="fa-solid fa-fill-drip"></i></p>`;
                this._html += `<div><input id="${this._gradientTopId}" type="color" value="${gradientEndColorHexString}" style="height:${gradientPickerHeight}px; width:30px;cursor:pointer;"/></div>`;
                this._html += `<div><input id="${this._gradientBottomId}" type="color" value="${gradientStartColorHexString}" style="height:${gradientPickerHeight}px; width:30px;cursor:pointer;"/></div>`;
                this._html += `</td>`;
            }

            //build pixels
            for (let x = 0; x < this._cols; x++) {
                let color = this._pixelColors[x][y];
                let colorHexString = this._helpers.rgbToHexString(color.R, color.G, color.B);
                this._html += `<td>
                <div style="width:${this._pixelWid}px;height:${this._pixelHgt}px;overflow:hidden;cursor:pointer;border-radius:20%;">
                <input type="color" value="${colorHexString}" data-px-x="${x}" data-px-y="${y}"
                class="pixel"  
                style=" 
                width:${this._pixelWid*3}px;
                height:${this._pixelWid*3}px;
                cursor: pointer;
                transform: translate(-50%, -50%);
                "></input>
                </div>
                </td>`;

            }

            this._html += '</tr>';
        }

        this._html += `</tbody>`;
        this._html += '</table>';
        this._html += `</div>`;

        return this._html;
    }

    //fired by deploy button
    buildDeployButton(){
        this._html += `<div id="deployButtonContainer" style="width: 100%; display: flex; flex-direction: row; justify-content: center; margin-top: 20px;">`;
        this._html += `<button id="${this._btnDeployId}" style="width:200px;height:100px;background-color:lightblue;cursor:pointer;font-weight:bolder;" onmouseover="this.style.backgroundColor='rgb(68, 127, 235)'" onmouseout="this.style.backgroundColor='lightblue'">Deploy</button>`;
        this._html += `</div>`;
        return this._html;
    }

}
