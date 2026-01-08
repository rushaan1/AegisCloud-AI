# Content Addressable Storage (CAS) Implementation Analysis

## Current Storage Architecture

### How Data is Stored

1. **Physical Storage**
   - Files are stored on the local filesystem at: `C:\CloudStoragePlatform\{UserId}\`
   - Each file is stored using its `FileId` (GUID) as the filename
   - No file extension is preserved in the physical storage
   - Example: A file with `FileId = "123e4567-e89b-12d3-a456-426614174000"` is stored as:
     ```
     C:\CloudStoragePlatform\{UserId}\123e4567-e89b-12d3-a456-426614174000
     ```

2. **Database Storage (SQL Server)**
   - **Files Table** stores:
     - `FileId` (GUID, Primary Key)
     - `FileName` (original filename with extension)
     - `FilePath` (logical path in the folder hierarchy)
     - `FileType` (enum: Image, Audio, Video, GIF, Document)
     - `Size` (in MB, float)
     - `ParentFolderId` (foreign key)
     - `MetadataId` (foreign key to metadata)
     - `SharingId` (foreign key to sharing)
     - User-specific fields (UserId, IsFavorite, IsTrash, CreationDate)

3. **Metadata Storage**
   - Separate `Metadatasets` table tracks:
     - OpenCount, RenameCount, MoveCount, ShareCount
     - LastOpened, PreviousRenameDate, PreviousMoveDate

### How Data is Sent to Clients

1. **File Preview/Streaming**
   - **Endpoint**: `GET /api/Retrievals/preview?filePath={path}`
   - **Process**:
     - Retrieves file by `FilePath` from database
     - Opens `FileStream` from physical path: `{PhysicalStoragePath}\{FileId}`
     - Streams directly to client with appropriate MIME type
     - Supports HTTP Range requests for partial content (via `RentedStreamResult`)
   - **Code Location**: `FileRetrievalService.GetFilePreview()`

2. **File Download**
   - **Endpoint**: `GET /api/Retrievals/download?fileIds={guids}&folderIds={guids}`
   - **Process**:
     - For single file: Streams directly from `{PhysicalStoragePath}\{FileId}`
     - For multiple files/folders: Creates ZIP archive on-the-fly
     - Each file in ZIP is read from `{PhysicalStoragePath}\{FileId}`
   - **Code Location**: `BulkRetrievalService.DownloadFolder()`

3. **Shared Content**
   - Public sharing endpoints use the same retrieval mechanism
   - Files are accessed by `FileId` from physical storage

### Current Limitations for CAS

1. **No Content Hashing**
   - No hash/checksum is computed during upload
   - No hash field exists in the database schema
   - Duplicate detection only checks file paths, not content

2. **FileId-Based Storage**
   - Files are stored by GUID, not by content hash
   - Same content uploaded multiple times = multiple physical copies
   - No deduplication at the storage level

3. **User-Specific Directories**
   - Each user has their own storage directory
   - Even if two users upload the same file, it's stored twice

## Content Addressable Storage (CAS) Feasibility Assessment

### ✅ **HIGHLY FEASIBLE** - Implementation Complexity: **Medium**

### Advantages of Current Architecture for CAS

1. **Clean Separation**
   - Physical storage is already abstracted from logical paths
   - Database already acts as a mapping layer (FilePath → FileId)
   - Easy to change the mapping (FilePath → ContentHash)

2. **Stream-Based Upload**
   - Files are already processed as streams during upload
   - Can compute hash during stream processing without additional I/O

3. **Repository Pattern**
   - Well-structured repository pattern makes changes localized
   - Can add hash computation and lookup logic without major refactoring

### Implementation Requirements

#### 1. Database Schema Changes
```sql
-- Add ContentHash column to Files table
ALTER TABLE [Files] ADD [ContentHash] nvarchar(64) NULL; -- SHA-256 hex string
CREATE INDEX IX_Files_ContentHash ON [Files]([ContentHash]);
```

#### 2. Storage Structure Changes

**Option A: Hash-Based Storage (Recommended)**
- Store files by hash: `C:\CloudStoragePlatform\{Hash[0:2]}\{Hash[2:4]}\{Hash}`
- Example: Hash `a1b2c3d4...` → `C:\CloudStoragePlatform\a1\b2\a1b2c3d4...`
- **Benefits**: Automatic deduplication, efficient storage
- **Challenges**: Migration of existing files

**Option B: Hybrid Approach (Easier Migration)**
- Keep FileId-based storage for existing files
- Use hash-based storage for new files
- Add hash lookup table for deduplication
- **Benefits**: Gradual migration, backward compatible
- **Challenges**: Two storage mechanisms to maintain

#### 3. Code Changes Required

**FileModificationService.UploadFile()** (Lines 40-122)
- Compute SHA-256 hash while copying stream
- Check if file with same hash exists
- If exists: Create new File record but reference existing physical file
- If not: Store file using hash as filename (or in hash-based directory)

**FileRetrievalService.GetFilePreview()** (Line 32)
- Change from: `{PhysicalStoragePath}\{FileId}`
- To: `{PhysicalStoragePath}\{ContentHash}` (or hash-based path)

**BulkRetrievalService.DownloadFolder()** (Lines 58, 89, 97)
- Update all file access to use ContentHash instead of FileId

**FilesRepository**
- Add method: `GetFileByContentHash(string hash)`
- Update `AddFile()` to handle hash-based storage

#### 4. Deduplication Logic

```csharp
// During upload:
1. Compute SHA-256 hash of file content
2. Check if file with same hash exists in database
3. If exists:
   - Create new File record with new FileId
   - Point to existing physical file (reference count?)
   - OR: Create hard link/symlink to existing file
4. If not exists:
   - Store file using hash as identifier
   - Create File record with hash
```

#### 5. Migration Strategy

**For Existing Files:**
1. Background job to:
   - Read each existing file
   - Compute hash
   - Update database with hash
   - Optionally: Move to hash-based storage structure
2. Or: Lazy migration (compute hash on first access)

### Implementation Complexity Breakdown

| Component | Complexity | Estimated Effort |
|-----------|-----------|------------------|
| Database Migration | Low | 1-2 hours |
| Hash Computation | Low | 2-3 hours |
| Upload Logic Changes | Medium | 4-6 hours |
| Retrieval Logic Changes | Medium | 3-4 hours |
| Deduplication Logic | Medium | 4-6 hours |
| Migration Script | Medium-High | 6-8 hours |
| Testing | Medium | 4-6 hours |
| **Total** | **Medium** | **24-35 hours** |

### Potential Challenges

1. **File Deletion**
   - Need reference counting or garbage collection
   - If multiple File records point to same physical file, don't delete until last reference removed

2. **User Isolation**
   - Currently each user has separate directory
   - CAS would share files across users (security consideration)
   - Solution: Keep user-specific directories OR implement access control on shared storage

3. **File Updates/Replacements**
   - If user "replaces" a file, it's actually a new upload
   - Old file should be garbage collected if no longer referenced

4. **Thumbnail Generation**
   - Thumbnails are currently stored by FileId
   - Need to update thumbnail service to use hash or keep FileId-based thumbnails

### Recommended Implementation Approach

**Phase 1: Foundation (Week 1)**
1. Add `ContentHash` column to database
2. Implement hash computation utility
3. Update `File` entity to include `ContentHash` property

**Phase 2: Upload Changes (Week 1-2)**
1. Modify `UploadFile()` to compute hash
2. Implement hash-based file storage
3. Add deduplication logic (reference counting)

**Phase 3: Retrieval Changes (Week 2)**
1. Update all file retrieval to use hash
2. Update thumbnail service if needed
3. Test file streaming and downloads

**Phase 4: Migration (Week 2-3)**
1. Create migration script for existing files
2. Run background job to compute hashes
3. Optionally migrate to hash-based directory structure

**Phase 5: Cleanup & Optimization (Week 3)**
1. Implement garbage collection for unreferenced files
2. Add monitoring/logging
3. Performance testing

### Conclusion

**Feasibility: ✅ HIGH**

The current architecture is well-suited for CAS implementation:
- Clean separation of concerns
- Stream-based processing already in place
- Repository pattern allows localized changes
- No major architectural refactoring needed

**Estimated Timeline: 2-3 weeks** for a complete implementation with testing and migration.

**Key Success Factors:**
1. Careful planning of storage structure (hash-based vs hybrid)
2. Proper reference counting for file deletion
3. Thorough testing of deduplication logic
4. Gradual migration strategy for existing files


