using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using ResultOf;

namespace FileSenderRailway
{
    public class FileSender
    {
        private readonly ICryptographer cryptographer;
        private readonly IRecognizer recognizer;
        private readonly Func<DateTime> now;
        private readonly ISender sender;

        public FileSender(
            ICryptographer cryptographer,
            ISender sender,
            IRecognizer recognizer,
            Func<DateTime> now)
        {
            this.cryptographer = cryptographer;
            this.sender = sender;
            this.recognizer = recognizer;
            this.now = now;
        }

        public IEnumerable<FileSendResult> SendFiles(FileContent[] files, X509Certificate certificate)
        {
            var signDocument = GetSignDocumentFunction();
            var validateDocument = GetValidateDocumentFunction();
            foreach (var file in files)
            {
                yield return SendFile(file, signDocument, validateDocument);
            }

            Func<Document, Document> GetSignDocumentFunction()
            {
                return document => document with { Content = cryptographer.Sign(document.Content, certificate) };
            }

            Func<Document, Result<Document>> GetValidateDocumentFunction()
            {
                return doc => ValidateDocument(doc, now().AddMonths(-1));
            }
        }

        private FileSendResult SendFile(FileContent file, Func<Document, Document> signDocument,
            Func<Document, Result<Document>> validateDocument)
        {
            var result = PrepareFileToSend(file,
                    signDocument,
                    recognizer.Recognize,
                    validateDocument)
                .Then(sender.Send)
                .RefineError("Can't prepare file to send");
            return new FileSendResult(file, result.Error);
        }

        public static Result<Document> PrepareFileToSend(FileContent file,
            Func<Document, Document> signDocument,
            Func<FileContent, Document> recognize,
            Func<Document, Result<Document>> validateDocument)
        {
            return file.AsResult()
                .Then(recognize)
                .Then(validateDocument)
                .Then(signDocument);
        }

        private static Result<Document> ValidateDocument(Document doc, DateTime monthBefore)
        {
            return IsValidFormatVersion(doc)
                .Then(_ => IsValidTimestamp(doc, monthBefore))
                .Then(_ => doc);
        }

        private static Result<bool> IsValidFormatVersion(Document doc)
        {
            return doc.Format is "4.0" or "3.1";
        }

        private static Result<bool> IsValidTimestamp(Document doc, DateTime monthBefore)
        {
            return doc.Created > monthBefore;
        }
    }
}