class WebDisplay{
    constructor(rows, cols, parentElement, helpers){
        this._helpers = helpers;
        this._parentElement = parentElement;
        this._winWid = window.innerWidth;
        this._winHgt = window.innerHeight;
        this._rows = rows;
        this._cols = cols;  
        this._pixelColors = [,];
        this._curLevels = [this._cols];
        this._colPeaks = [this._cols];
        this._peakColor = 'red';
        this._blackPixel = 'black';
        this._tblMatrixId = 'tblMatrix';
        this._colPeaks = [];
        this._transitionSpeed = 2;
        this._peakWait = 500;
        this._peakWaitCountDown = 20;
        this._showPeaks = true;
        this._pixelWid = this._winWid / (this._cols * 2);
        this._pixelHgt =  (1 / this._rows) * 250;

        for (let i = 0; i < this._cols; i++) {
            this._curLevels.push(0);
            this._colPeaks.push({ col: i, row: 0, curWait: 0, curTime: 0, prevTime: 0  });
        }

        this.buildMatrix();
        this.setupDefaultColors();
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
        for (let x = 0; x < this._cols; x++) {
            if(targetLevels[x].Level > this._curLevels[x])
            {
                this._curLevels[x] = targetLevels[x].Level;                    
            }else if (targetLevels[x].Level < this._curLevels[x]){
                this._curLevels[x] = Math.max(this._curLevels[x] - this._transitionSpeed, targetLevels[x].Level);  //bring down gradually
            }
            
            for (let y = 0; y < this._rows; y++)
            {
                let color = '';                                            
                if(y < this._curLevels[x] ){
                    color =  this._pixelColors[x, y]; 
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
        let topRowIndex = this._rows - 1;

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
        let color = '';       
        if(this._colPeaks[col].row >= 0) //if value (x) not at bottom, set peak color of the row
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

        for (let y = this._rows-1; y >= 0; y--) {  
            let hue = this._helpers.map(y, 0, this._rows, 100, 0);
            let color =  `hsl(${hue}, 100%, 50%)`;  
            matrixHtml += '<tr>';
            for (let x = 0; x < this._cols; x++) {
                this._pixelColors[x, y] = color;

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
        for (let x = 0; x < this._cols; x++) {
            matrixHtml += `<td data-band-x="${x}">${x}</td>`;
        }
        matrixHtml += '</tr>';

        matrixHtml += '</table>';

        $(this._parentElement).html(matrixHtml);
    }

    setupDefaultColors()
    {
        for (let x = 0; x < this._cols; x++)
        {
            for (let y = 0; y < this._rows; y++)
            {
                let hueIndex = this._helpers.map(y, 0, this._rows-1, 100, 0); //map row numbers to the color range green to red
                this._pixelColors[x, y] =  `hsl(${hueIndex}, 100%, 50%)`;
            }            
        }
    }
}
