import { useState } from 'react';
import axios from 'axios';
import './custom.css';

const App = () => {
    const [file, setFile] = useState(null);
    const [recentlyAddedFile, setRecentlyAddedFile] = useState(null);

    const onFileChange = e => {
        setFile(e.target.files[0]);
    }

    const onFileUpload = async () => {
        const formData = new FormData();
        formData.append('file', file);

        const result = await axios.post("api/file/upload", formData);
        const id = result.data.id;
        const name = result.data.name;
        setRecentlyAddedFile({ id, name });
    }

    const onFileDownload = async () => {
        const response = await axios({
            url: `api/file/download/${recentlyAddedFile.id}`,
            method: 'GET',
            responseType: 'blob'
        });

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
            <button onClick={onFileUpload}>Upload</button>

            {recentlyAddedFile &&
                <div>
                    <p><span>{recentlyAddedFile.name}</span> has been successfully uploaded</p>
                    <button onClick={onFileDownload}>Download {recentlyAddedFile.name}</button>
                </div>
            }
        </div>
    );
}

export default App