const docsBase = 'https://github.com/ricetim/readarr-rresurrected/blob/develop/docs';

// Links into this project's documentation in docs/. Anchors are GitHub's heading slugs;
// .github/scripts/check_docs_anchors.py checks that every one used here exists.
export default function getDocsUrl(page, anchor) {
  const url = `${docsBase}/${page}.md`;

  return anchor ? `${url}#${anchor}` : url;
}
