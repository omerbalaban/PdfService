import pdfkit
import base64

def html_to_pdf(base64_string,page_size="A4",orientation="Portrait"):
    options = {
        'page-size': page_size,
        'margin-top': '0.75in',
        'margin-right': '0.75in',
        'margin-bottom': '0.75in',
        'margin-left': '0.75in',
        'orientation': orientation,
        'encoding': "UTF-8",
    }

    # Convert HTML to PDF
    pdf = pdfkit.from_string(base64_to_string(base64_string), False, options=options)
    return pdf


def base64_to_string(base64_string):
    decoded_bytes = base64.b64decode(base64_string)
    decoded_string = decoded_bytes.decode('utf-8')  # Assuming UTF-8 encoding, adjust if necessary
    return decoded_string