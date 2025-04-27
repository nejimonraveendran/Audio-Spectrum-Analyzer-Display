class WebDisplay{
    constructor(config, parentElement, helpers){
        this._config = config;
        this._helpers = helpers;
        this._parentElement = parentElement;
        this._winWid = window.innerWidth;
        this._winHgt = window.innerHeight;

        this._pixelColors = [];
        this._curLevels = [];
        this._peakColor = this._helpers.rgbToHexString(this._config.PeakColor.R, this._config.PeakColor.G, this._config.PeakColor.B); // 'red';
        
        this._blackPixel = 'black';
        this._tblMatrixId = 'tblMatrix';
        this._colPeaks = [];
        this._transitionSpeed = this._config.TransitionSpeed;
        this._peakWait = this._config.PeakWait;
        this._peakWaitCountDown = this._config.PeakWaitCountDown;
        this._showPeaks = this._config.ShowPeaks;
        
        this._pixelWid = this._winWid / (this._config.Cols * 2);
        this._pixelHgt =  (1 / this._config.Rows) * 250;

        this.populatePixelColors();

        for (let i = 0; i < this._config.Cols; i++) {
            this._curLevels.push(0);
            this._colPeaks.push({ col: i, row: 0, curWait: 0, curTime: 0, prevTime: 0  });
        }

        this.buildMatrix();
    }

    //property setters
    setPeakColor(color){
        this._peakColor = color;
    }

    setTransitionSpeed(speed){
        this._transitionSpeed = speed;
    }

    setShowPeaks(show){
        this._showPeaks = show;
    }

    setPkeakWait(wait){
        this._peakWait = wait;
    }
    
    setPeakWaitCountDown(countDown){
        this._peakWaitCountDown = countDown;
    }

    clear(){
        let allPixels = $(`#${this._tblMatrixId} tr td`); 
        if(allPixels === null || allPixels === undefined) return;
        allPixels.css('backgroundColor', this._blackPixel);
    }

    displayLevels(targetLevels){
        for (let x = 0; x < this._config.Cols; x++) {
            if(targetLevels[x].Level > this._curLevels[x])
            {
                this._curLevels[x] = targetLevels[x].Level;                    
            }else if (targetLevels[x].Level < this._curLevels[x]){
                this._curLevels[x] = Math.max(this._curLevels[x] - this._transitionSpeed, targetLevels[x].Level);  //bring down gradually
            }
            
            for (let y = 0; y < this._config.Rows; y++)
            {
                let color = '';                                            
                if(y < this._curLevels[x] ){
                    color =  this._pixelColors[x][y]; 
                }else{
                    color = this._blackPixel;                        
                }     
                
                let colPixel = $(`#${this._tblMatrixId} tr td[data-px-x="${x}"][data-px-y="${y}"]`); 
                colPixel.css('backgroundColor', color);
            }

            let bandInfo = $(`#${this._tblMatrixId} tr td[data-band-x="${x}"]`);
            bandInfo.html(targetLevels[x].Band >= 1000 ? (targetLevels[x].Band/1000) + 'KHz' :  targetLevels[x].Band + 'Hz' );


            if(this._showPeaks){
                this.setPeaks(x, this._curLevels[x]);
            }

        }    
    }

    setPeaks(col, value){
        let topRowIndex = this._config.Rows - 1;

        if(value > this._colPeaks[col].row)  //set new peaks if current value is greater than previously stored peak.
        {
            this._colPeaks[col].row = value; //set peak at one above the actual value.

            if(this._colPeaks[col].row > topRowIndex)  //dont let it overflow the top row index
            { 
                this._colPeaks[col].row = topRowIndex;
            }

            this._colPeaks[col].curTime = this._colPeaks[col].prevTime = Date.now();
            this._colPeaks[col].curWait = this._peakWait;

        }

        //set peaks
        let targetPeakRow = 1;
        if(this._config.ShowPeaksWhenSilent){
            targetPeakRow = 0;
        }

        let color = '';       
        if(this._colPeaks[col].row > targetPeakRow) //if value (x) not at bottom, set peak color of the row
        {
            color = this._peakColor;
        }
        else //otherwise set to black. 
        { 
            color = this._blackPixel;
        }

        let colPixel = $(`#${this._tblMatrixId} tr td[data-px-x="${col}"][data-px-y="${this._colPeaks[col].row}"]`); 
        colPixel.css('backgroundColor', color);


        //logic for the peaks to fall down
        this._colPeaks[col].curTime = Date.now();

        if (this._colPeaks[col].curTime - this._colPeaks[col].prevTime >= this._colPeaks[col].curWait)
        {
            if(this._colPeaks[col].row > 0)
            {
                this._colPeaks[col].row -= 1; //deduct one row (creates fall down effect)
                this._colPeaks[col].prevTime = this._colPeaks[col].curTime;

            }

        }

        this._colPeaks[col].curWait -=  this._peakWaitCountDown;


        if(this._colPeaks[col].curWait < this._peakWaitCountDown)
        {
            this._colPeaks[col].curWait = this._peakWaitCountDown;
            
        }

    }

    
    buildMatrix(){        
        let matrixHtml = `<table id="${this._tblMatrixId}">`;

        for (let y = this._config.Rows-1; y >= 0; y--) {  
            // let hue = this._helpers.map(y, 0, this._config.Rows, 100, 0);
            // let color =  `hsl(${hue}, 100%, 50%)`;  
            matrixHtml += '<tr>';
            for (let x = 0; x < this._config.Cols; x++) {
                // this._config.PixelColors[x][y] = color;
                matrixHtml += `<td data-px-x="${x}" data-px-y="${y}"  
                style="min-width:${this._pixelWid}px; 
                min-height:${this._pixelHgt}px; 
                width: ${this._pixelWid}px;
                height: ${this._pixelHgt}px;
                border-right: ${10}px; 
                border-style: solid;
                border-color: rgb(10, 10, 10);
                background-color: rgb(5, 5, 5);
                "></td>`;
            }

            matrixHtml += '</tr>';
        }

        //display bands
        matrixHtml += '<tr>';
        for (let x = 0; x < this._config.Cols; x++) {
            matrixHtml += `<td data-band-x="${x}">${x}</td>`;
        }
        matrixHtml += '</tr>';

        matrixHtml += '</table>';

        $(this._parentElement).html(matrixHtml);
    }

    populatePixelColors()
    {
        for (let x = 0; x < this._config.Cols; x++)
        {
            let columnPixels = [];
            for (let y = 0; y < this._config.Rows; y++)
            {
                let color = this._config.PixelColors[x][y]; 
                columnPixels.push(this._helpers.rgbToHexString(color.R, color.G, color.B));
            }            

            this._pixelColors.push(columnPixels);
        }
    }
}
