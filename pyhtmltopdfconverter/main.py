import uvicorn
from fastapi import FastAPI, HTTPException, File, UploadFile
from fastapi.responses import RedirectResponse,JSONResponse
from pydantic import BaseModel
from typing import List, Optional
from pypdf import PdfReader
import pdfconverter
import base64

app = FastAPI(title="WkhtmltoPdf Converter")


class CustomPropertyItem(BaseModel):
    Key: str
    Value: str


class PDFRequest(BaseModel):
    CorrelationId: str
    PageSize: str
    PageOrientation: str
    Margins: int
    PdfConverter: int
    DocumentTitle: str = ""
    Zoom: Optional[int] = 90
    Base64HtmlContent: str
    CustomPropertyItems: Optional[List[CustomPropertyItem]] = []


class PDFResponse(BaseModel):
    Success: bool
    Message: Optional[str] = None
    Id: str
    Content: str = ""


@app.get("/")
async def doc_redirect():
    return RedirectResponse(url='/docs')


@app.get("/api-docs")
async def doc_redirect():
    return RedirectResponse(url='/docs')


@app.get("/diagnose")
async def diagnose():
    return {"Status": "SUCCESS", "StatusMessage": f"Convert to Pdf OK"}


@app.post("/api/Pdf/ConvertHtmlToPdf", response_model=PDFResponse)
async def generate_pdf(pdf_request: PDFRequest):
    pdf = pdfconverter.html_to_pdf(pdf_request.Base64HtmlContent, pdf_request.PageSize, pdf_request.PageOrientation)
    response = PDFResponse(Success=True, Id=pdf_request.CorrelationId, Message="Success", Content=base64.b64encode(pdf))
    return response

@app.post("/extract-pdf-lines/")
async def extract_pdf_lines(file: UploadFile = File(...)):
    try:
        pdf_reader = PdfReader(file.file)
        lines = []
        for page in pdf_reader.pages:
            lines.extend(page.extract_text().splitlines(False))
        return {"lines": lines}
    except Exception as e:
        return JSONResponse(
            status_code=500,
            content={"error": str(e)},
        )

if __name__ == "__main__":
    print("Uvicorn Start")
    uvicorn.run(app, host="0.0.0.0", port=8000)
