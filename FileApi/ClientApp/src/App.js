import { useState } from 'react';
import axios from 'axios';
import './custom.css';
import ClipLoader from "react-spinners/ClipLoader";

const App = () => {
    const [file, setFile] = useState(null);
    const [note, setNote] = useState("");
    const [recentlyAddedFile, setRecentlyAddedFile] = useState(null);
    const [loading, setLoading] = useState(false);
    const [completedUpload, setCompletedUpload] = useState(false);
    const [completedDownload, setCompletedDownload] = useState(false);

    const onFileChange = e => {
        setFile(e.target.files[0]);
    }

    const onNoteChange = e => {
        setNote(e.target.value);
    }

    const onFileUpload = async () => {
        const formData = new FormData();
        formData.append('file', file);
        formData.append('note', note);

        setCompletedDownload(false);
        setLoading(true);
        const result = await axios.post("api/File/upload", formData);
        setLoading(false);
        const id = result.data.id;
        const filename = result.data.filename;
        setRecentlyAddedFile({ id, filename });
        setCompletedUpload(true);
        setNote("");
    }

    const onFileDownload = async () => {
        setCompletedUpload(false);
        setLoading(true);
        const response = await axios({
            url: `api/File/download/${recentlyAddedFile.id}`,
            method: 'GET',
            responseType: 'blob'
        });
        setLoading(false);
        setCompletedDownload(true);

        const url = window.URL.createObjectURL(new Blob([response.data]));
        const link = document.createElement('a');
        link.href = url;

        // Extract the filename from the Content-Disposition header
        const contentDisposition = response.headers['content-disposition'];
        const fileName = contentDisposition.split('filename=')[1].split(';')[0];
        link.setAttribute('download', fileName);

        document.body.appendChild(link);
        link.click();
    }

    return (
        <div className="container">
            <h1>Upload/Download Files Using ASP.NET Core Web API - <span>Corey Brown</span></h1>
            <input type="file" onChange={onFileChange} />

            <div style={{ margin: "1rem" }}>
                <label for="note" style={{ marginRight: "0.5rem" }}>Add A Note (optional):</label>
                <textarea id="note" name="note"  rows={3} value={note} onChange={onNoteChange} />
            </div>

            <button onClick={onFileUpload}>Upload</button>

            <div style={{ margin: "4rem" }}>
                <ClipLoader
                    loading={loading}
                    color="blue"
                    size={75}
                />
            </div>

            {(completedUpload && !loading) &&
                <div style={{margin: "4rem"} }>
                    <p><span>{recentlyAddedFile.filename}</span> has been successfully uploaded</p>
                    <button onClick={onFileDownload}>Download {recentlyAddedFile.name}</button>
                </div>
            }

            {(completedDownload && !loading) &&
                <div style={{margin:"4rem"} }>
                    <p>File has been successfully downloaded</p>
                </div>
            }
        </div>
    );
}

export default App