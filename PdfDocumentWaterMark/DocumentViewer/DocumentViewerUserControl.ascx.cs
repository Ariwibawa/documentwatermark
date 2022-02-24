using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

namespace PdfDocumentWaterMark.DocumentViewer
{
    public partial class DocumentViewerUserControl : UserControl
    {
        private string ParamUrlDocument
        {
            get
            {
                if (string.IsNullOrEmpty(Request.QueryString["Url"]))
                    return "#";
                else
                {
                    var fullPath = Request.Url.Query.Substring(Request.Url.Query.IndexOf("Url=") + 4);
                    var fileName = fullPath.Substring(fullPath.LastIndexOf("/") + 1);
                    if (fileName.IndexOf("+") == 0)
                        fileName = Uri.EscapeDataString(fileName);
                    return fullPath.Substring(0, fullPath.LastIndexOf("/")) + "/" + fileName;
                }
            }
        }
        private void AddWaterMark(System.IO.Stream inputStream, string waterMarkText, System.IO.Stream outPutStream, int spaceBetweenText = 15, int fontSize = 20, int gapBetweenText = 150)
        {
            PdfDocument pdfDoc = new PdfDocument(new PdfReader(inputStream), new PdfWriter(outPutStream));
            Document document = new Document(pdfDoc);
            Rectangle pageSize;
            PdfCanvas canvas;
            int n = pdfDoc.GetNumberOfPages();
            waterMarkText = String.Join(new string(' ', spaceBetweenText), Enumerable.Repeat(waterMarkText, 10));
            for (int i = 1; i <= n; i++)
            {
                PdfPage page = pdfDoc.GetPage(i);
                pageSize = page.GetPageSize();
                canvas = new PdfCanvas(page);
                Paragraph p = new Paragraph(waterMarkText).SetFontSize(fontSize);
                canvas.SaveState();
                PdfExtGState gs1 = new PdfExtGState().SetFillOpacity(0.2f);
                canvas.SetExtGState(gs1);
                for (float j = 1; j < 8; j++)
                    document.ShowTextAligned(p, 1000 - (j * gapBetweenText), j * gapBetweenText, pdfDoc.GetPageNumber(page), TextAlignment.CENTER, VerticalAlignment.MIDDLE, 120);
                canvas.RestoreState();
            }
            pdfDoc.Close();

        }
        public void CopyTo(Stream inStream, Stream outStream, long length)
        {
            CopyTo(inStream, outStream, length, 4096);
        }

        public void CopyTo(Stream inStream, Stream outStream, long length, int blockSize)
        {
            byte[] buffer = new byte[blockSize];
            long currentPosition = 0;

            while (true)
            {
                int read = inStream.Read(buffer, 0, blockSize);
                if (read == 0) break;
                long cPosition = currentPosition + read;
                if (cPosition > length) read = read - Convert.ToInt32(cPosition - length);
                outStream.Write(buffer, 0, read);
                currentPosition += read;
                if (currentPosition >= length) break;
            }
        }
        public void ProcessDocument()
        {
            if (ParamUrlDocument.EndsWith(".pdf"))
            {
                //TODO: Impersonate user from SharePoint CurrentUser
                //var wi = Microsoft.SharePoint.SPContext.Current.Web.CurrentUser;
                //var impersonationContext = WindowsIdentity.Impersonate(wi.UserToken.);
                var webPdf = new WebClient
                {
                    UseDefaultCredentials = true,
                    CachePolicy = new System.Net.Cache.RequestCachePolicy(RequestCacheLevel.BypassCache)
                };
                //temporary solution, use user and password to connect
                webPdf.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["ConnectUserName"], ConfigurationManager.AppSettings["ConnectPassword"], ConfigurationManager.AppSettings["ConnectDomain"]);
                var waterMarkText = Microsoft.SharePoint.SPContext.Current.Web.CurrentUser.Email;
                var fileName = ParamUrlDocument.Substring(ParamUrlDocument.LastIndexOf('/') + 1);
                Response.AppendHeader("Content-Disposition", "attachment;filename=\"" + fileName + "\"");
                Response.ContentType = "application/pdf";
                AddWaterMark(webPdf.OpenRead(ParamUrlDocument), waterMarkText, Response.OutputStream);
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            ProcessDocument();
        }
    }
}
